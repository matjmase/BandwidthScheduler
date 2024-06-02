using Azure.Core;
using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Role;
using BandwidthScheduler.Server.Common.Static;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.Controllers.Validation;
using BandwidthScheduler.Server.Controllers.Validation.Schedule;
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

namespace BandwidthScheduler.Server.Controllers.Schedule
{
    [Route("api/schedule/[controller]")]
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

            var totalAvailabilities = await AvailabilityController.GetTeamAvailabilities(proposalRequest.SelectedTeam.Id, windowStart, windowEnd, _db);
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

            var totalApplicable = await AvailabilityController.GetTeamAvailabilities(submitRequest.ProposalRequest.SelectedTeam.Id, windowStart, windowEnd, _db);

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
            var capturedTws = await CommitmentController.GetCommitmentTimeWindowsCaptured(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var capturedTwsDict = capturedTws.ToDictionaryAggregate(e => e.UserId);

            foreach (var kv in proposalDict)
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

            var encapsulateArr = await CommitmentController.GetTimeWindowCommitmentEncapsulated(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var leftArr = await CommitmentController.GetCommitmentLeftNeighbor(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();
            var rightArr = await CommitmentController.GetCommitmentRightNeighbor(_db.Commitments, proposalDict.Keys, teamId, windowStart, windowEnd).ToArrayAsync();

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


        
    }
}
