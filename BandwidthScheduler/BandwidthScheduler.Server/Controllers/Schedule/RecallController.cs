﻿using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.Controllers.Validation.Schedule;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BandwidthScheduler.Server.Controllers.Schedule
{
    [Route("api/schedule/[controller]")]
    [ApiController]
    public class RecallController : ControllerBase
    {
        private IConfiguration _config;
        private BandwidthSchedulerContext _db;

        public RecallController(BandwidthSchedulerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> Recall([FromBody] ScheduleRecallRequest recallRequest)
        {
            // Validate request

            if (!RecallControllerValidation.ValidateRecallRequest(recallRequest))
            {
                return BadRequest("Invalid Proposal");
            }

            var start = recallRequest.Start.ToUniversalTime();
            var end = recallRequest.End.ToUniversalTime();
            var teamId = recallRequest.TeamId;

            // get all intersection

            var intersection = await ScheduleController.GetCommitmentAnyIntersection(_db.Commitments, recallRequest.TeamId, start, end).Include(e => e.User).ToArrayAsync();

            // stitching

            var userIds = intersection.Select(e => e.UserId).Distinct();
            
            var encapsulateArr = await ScheduleController.GetTimeWindowCommitmentEncapsulated(_db.Commitments, userIds, teamId, start, end).ToArrayAsync();
            var leftArr = await ScheduleController.GetCommitmentLeftNeighbor(_db.Commitments, userIds, teamId, start, end).ToArrayAsync();
            var rightArr = await ScheduleController.GetCommitmentRightNeighbor(_db.Commitments, userIds, teamId, start, end).ToArrayAsync();

            var encapsulateDict = encapsulateArr.ToDictionary(e => e.UserId);
            var leftDict = leftArr.ToDictionary(e => e.UserId);
            var rightDict = rightArr.ToDictionary(e => e.UserId);

            var toAddCommitment = new HashSet<Commitment>();
            var toRemoveCommitment = new HashSet<Commitment>();

            var encapsulatedMiddle = new HashSet<Availability>();

            foreach (var userId in userIds)
            {
                var encapsulate = encapsulateDict.GetValueOrDefault(userId);    
                var left = leftDict.GetValueOrDefault(userId);    
                var right = rightDict.GetValueOrDefault(userId);

                if (!TimeFrameFunctions.StitchSidesCommitment(userId, teamId, start, end, encapsulate, left, right, out var toAddStitch, out var toRemoveStitch))
                {
                    return BadRequest("Database corrupted");
                }

                toAddCommitment.AddRange(toAddStitch);
                toRemoveCommitment.AddRange(toRemoveStitch);

                if (encapsulate != null)
                {
                    encapsulatedMiddle.Add(new Availability()
                    {
                        UserId = userId,
                        StartTime = start,
                        EndTime = end,
                    });
                }
            }

            // captured transition

            var captured = await ScheduleController.GetCommitmentTimeWindowsCaptured(_db.Commitments, userIds, teamId, start, end).ToArrayAsync();

            // get current availabilities

            var intersectingAvailabilities = await ScheduleController.GetAvailabilities(teamId, start, end, _db);

            // streak them together

            var intersectingAvailabilitiesDictionary = intersectingAvailabilities.ToDictionaryAggregate(e => e.UserId);
            var capturedAsAvailability = captured.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime }).ToDictionaryAggregate(e => e.UserId);

            var toAddAvail = new HashSet<Availability>();
            var toRemoveAvail = new HashSet<Availability>();

            foreach (var userId in capturedAsAvailability.Keys)
            {
                var capturedAsAvailForUser = capturedAsAvailability[userId];
                var intersectingAvailsForUser = intersectingAvailabilitiesDictionary.GetValueOrDefault(userId) ?? new Availability[0];

                if (!TimeFrameFunctions.CreateStreaksAvailability(intersectingAvailsForUser.Union(capturedAsAvailForUser), out var streaks) || streaks == null)
                {
                    return BadRequest("collision of proposal and db commitments");
                }

                TimeFrameFunctions.IdentifyRedundancyAvailability(streaks, intersectingAvailsForUser, out var toAdd, out var toRemove);

                toAddAvail.AddRange(toAdd);
                toRemoveAvail.AddRange(toRemove);
            }

            // finalize with db

            _db.Commitments.RemoveRange(captured);
            _db.Commitments.RemoveRange(toRemoveCommitment);
            _db.Availabilities.RemoveRange(toRemoveAvail);

            await _db.Commitments.AddRangeAsync(toAddCommitment);
            await _db.Availabilities.AddRangeAsync(toAddAvail);
            await _db.Availabilities.AddRangeAsync(encapsulatedMiddle);

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
