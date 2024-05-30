using Azure.Core;
using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Role;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.Controllers.Validation;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace BandwidthScheduler.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PublishController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public PublishController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("commitments")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> GetCommitments([FromHeader(Name = "start")] string startString, [FromHeader(Name = "end")] string endString, [FromHeader(Name = "teamId")] int teamId)
        {
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            try
            {
                start = DateTime.Parse(startString);
                end = DateTime.Parse(endString);
            }
            catch
            {
                return BadRequest("start and or end could not be parsed");
            }

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var current = DbModelFunction.GetCurrentUser(HttpContext);

            var commitments = await GetCommitmentAnyIntersection(_db.Commitments, teamId, start, end).Include(e => e.User).ToArrayAsync();

            commitments.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());
            commitments.Foreach(e => e.NullifyRedundancy());

            return Ok(commitments);
        }

        [HttpPost("proposal")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> Proposal([FromBody] ScheduleProposalRequest proposalRequest)
        {
            // Validate Proposal

            if (!PublishControllerValidation.ValidateProposalRequest(proposalRequest, out var windowStart, out var windowEnd))
            {
                return BadRequest("Invalid Proposal");
            }

            // Scope availabilities to proposal window

            var totalAvailabilities = await GetAvailabilities(proposalRequest.SelectedTeam.Id, windowStart, windowEnd);
            var userAvailabilityArrays = totalAvailabilities.ToDictionaryAggregate(e => e.UserId);

            var output = ScheduleGeneration.ScopeStreakToWindow(userAvailabilityArrays, proposalRequest.Proposal);

            return Ok(new ScheduleProposalResponse() { ProposalUsers = output.ToArray() });
        }


        [HttpPost("submit")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> ProposalSubmit([FromBody] ScheduleSubmitRequest submitRequest)
        {
            // Validate Proposal

            if (!PublishControllerValidation.ValidateProposalSubmitRequest(submitRequest, out var windowStart, out var windowEnd))
            {
                return BadRequest("Invalid Proposal");
            }

            // Reproducibility checking

            if (!PublishControllerValidation.ProposalSubmitReproducibilityCheck(submitRequest))
            {
                return BadRequest("Request and Response data has been altered");
            }

            // DB Checking.

            var totalApplicable = await GetAvailabilities(submitRequest.ProposalRequest.SelectedTeam.Id, windowStart, windowEnd);

            if (totalApplicable == null)
            {
                return BadRequest("Invalid Proposal");
            }

            if (!PublishControllerValidation.ProposalSubmitDatabaseCheck(totalApplicable, submitRequest, out var removedAvailabilities, out var addedAvailabilities, out var totalProposals) || removedAvailabilities == null || addedAvailabilities == null || totalProposals == null)
            {
                return BadRequest("Availabilites have changed");
            }

            // validation done, merge with other commitments

            var toRemoveCommitments = new HashSet<Commitment>();

            var teamId = submitRequest.ProposalRequest.SelectedTeam.Id;

            var proposalDict = totalProposals.ToDictionaryAggregate(e => e.UserId);
            var capturedTws = await GetCommitmentTimeWindowsCaptured(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var capturedTwsDict = capturedTws.ToDictionaryAggregate(e => e.UserId);

            foreach(var kv in proposalDict)
            {
                var userId = kv.Key;

                if (!capturedTwsDict.ContainsKey(userId))
                {
                    continue;
                }

                var dbCommitments = capturedTwsDict[userId];
                var proposal = kv.Value;

                if (!TimeFrameFunctions.CreateStreaksCommitment(proposal.Union(dbCommitments), out var streaks) || streaks == null)
                {
                    return BadRequest("collision of proposal and db commitments");
                }

                TimeFrameFunctions.IdentifyRedundancyCommitment(streaks, dbCommitments, out var toAdd, out var toRemove);

                proposalDict[userId] = toAdd.ToArray();
                toRemoveCommitments.AddRange(toRemove);
            }

            // stitch sides

            var encapsulateArr = await GetTimeWindowCommitmentEncapsulated(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var leftArr = await GetCommitmentLeftNeighbor(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var rightArr = await GetCommitmentRightNeighbor(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();

            var encapsulateDict = encapsulateArr.ToDictionary(e => e.UserId);
            var leftDict = leftArr.ToDictionary(e => e.UserId);
            var rightDict = rightArr.ToDictionary(e => e.UserId);

            foreach (var kv in proposalDict)
            {
                var userId = kv.Key;

                var encapsulate = encapsulateDict.GetValueOrDefault(userId);
                var left = leftDict.GetValueOrDefault(userId);
                var right = rightDict.GetValueOrDefault(userId);

                if (!TimeFrameFunctions.StitchSidesCommitment(userId, teamId, windowStart, windowEnd, encapsulate, left, right, out var toAdd, out var toRemove))
                {
                    return BadRequest("Database corrupted");
                }

                if (!TimeFrameFunctions.CreateStreaksCommitment(kv.Value.Union(toAdd), out var streaks) || streaks == null) 
                {
                    return ValidationProblem("Internal Error");
                }

                proposalDict[userId] = streaks.ToArray();
                toRemoveCommitments.AddRange(toRemove);
            }

            // Put results in DB

            totalProposals = proposalDict.SelectMany(e => e.Value).ToArray();

            _db.Availabilities.RemoveRange(_db.Availabilities.Where(e => removedAvailabilities.Select(a => a.Id).Contains(e.Id)));
            await _db.Availabilities.AddRangeAsync(addedAvailabilities);
            _db.Commitments.RemoveRange(toRemoveCommitments);
            await _db.Commitments.AddRangeAsync(totalProposals);

            await _db.SaveChangesAsync();

            return Ok();
        }

        #region Queries

        [NonAction]
        public async Task<Availability[]> GetAvailabilities(int teamId, DateTime windowStart, DateTime windowEnd)
        {
            var totalApplicable = await _db.UserRoles
                .Where(e => e.RoleId == (int)AuthenticationRole.User) // role filtering
                .Include(e => e.User).ThenInclude(e => e.UserTeams) // userteam include
                .Where(e => e.User.UserTeams.Any(e => e.TeamId == teamId)) // userteam filter
                .Include(e => e.User).ThenInclude(e => e.Availabilities).ThenInclude(e => e.User) // availabilities include with user
                .Select(e => e.User).SelectMany(e => e.Availabilities) // availabilies nav
                .Where(e => !(e.EndTime <= windowStart || e.StartTime >= windowEnd)).OrderBy(e => e.StartTime).ToArrayAsync(); // availability filter

            totalApplicable.Foreach(e => e.ExplicitlyMarkDateTimesAsUtc());

            return totalApplicable;
        }

        [NonAction]
        private static IQueryable<Commitment> GetTimeWindowCommitmentEncapsulated(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentTimeWindowEncapsulatedExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentTimeWindowEncapsulatedExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&
            (
                e.StartTime < start && e.EndTime > end          // encapsulated
            );
        }

        [NonAction]
        private static IQueryable<Commitment> GetCommitmentRightNeighbor(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentRightNeighborExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentRightNeighborExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&
            (
                e.EndTime > end && e.StartTime >= start && e.StartTime <= end        // caught the start
            );
        }

        [NonAction]
        private static IQueryable<Commitment> GetCommitmentLeftNeighbor(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentLeftNeighborExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentLeftNeighborExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&
            (
                e.StartTime < start && e.EndTime >= start && e.EndTime <= end               // caught the end
            );
        }

        [NonAction]
        private static IQueryable<Commitment> GetCommitmentTimeWindowsCaptured(DbSet<Commitment> db, IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return db.Where(CommitmentTimeWindowsCapturedExpression(userIds, teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentTimeWindowsCapturedExpression(IEnumerable<int> userIds, int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            userIds.Contains(e.UserId) &&
            (
                start <= e.StartTime && end >= e.EndTime            // captured
            );
        }

        [NonAction]
        private static IQueryable<Commitment> GetCommitmentAnyIntersection(IQueryable<Commitment> db, int teamId, DateTime start, DateTime end)
        {
            return db.Include(e => e.User).Include(e => e.Team).Where(CommitmentIntersectionExpression(teamId, start, end));
        }

        [NonAction]
        public static Expression<Func<Commitment, bool>> CommitmentIntersectionExpression(int teamId, DateTime start, DateTime end)
        {
            return e =>
            e.TeamId == teamId &&
            !(
                (e.EndTime <= start) || e.StartTime >= end
            );
        }
        #endregion
    }
}
