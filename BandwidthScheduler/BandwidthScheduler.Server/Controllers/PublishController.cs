using Azure.Core;
using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Role;
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

            var output = ScopeStreakToWindow(userAvailabilityArrays, proposalRequest.Proposal);

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

            if (!ProposalSubmitReproducibilityCheck(submitRequest))
            {
                return BadRequest("Request and Response data has been altered");
            }

            // DB Checking.

            var totalApplicable = await GetAvailabilities(submitRequest.ProposalRequest.SelectedTeam.Id, windowStart, windowEnd);

            if (totalApplicable == null)
            {
                return BadRequest("Invalid Proposal");
            }

            if (!ProposalSubmitDatabaseCheck(totalApplicable, submitRequest, out var removedAvailabilities, out var addedAvailabilities, out var totalProposals) || removedAvailabilities == null || addedAvailabilities == null || totalProposals == null)
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

        [NonAction]
        private bool MergeCommitments(IEnumerable<Commitment> insert, IEnumerable<Commitment> captured, out List<Commitment> result)
        {
            result = new List<Commitment>();

            var captureEnum = captured.GetEnumerator();
            var insertEnum = insert.GetEnumerator();

            var captureHasNext = captureEnum.MoveNext();
            var insertHasNext = insertEnum.MoveNext();

            Func<Commitment, Commitment, bool> intersection = (f, s) => !(f.EndTime <= s.StartTime || f.StartTime >= s.EndTime);

            Commitment? currentCommitment = null;

            Func<Commitment, List<Commitment>, bool> processCommitment = (c, o) =>
            {
                if (currentCommitment == null)
                {
                    currentCommitment = new Commitment() { UserId = c.UserId, TeamId = c.TeamId, StartTime = c.StartTime, EndTime = c.EndTime };
                }
                else if (intersection(currentCommitment, c))
                {
                    return false;
                }
                else if (currentCommitment.EndTime == c.StartTime)
                {
                    currentCommitment.EndTime = c.EndTime;
                }
                else
                {
                    o.Add(currentCommitment);
                    currentCommitment = new Commitment() { UserId = c.UserId, TeamId = c.TeamId, StartTime = c.StartTime, EndTime = c.EndTime };
                }

                return true;
            };

            while (captureHasNext && insertHasNext)
            {
                var captureValue = captureEnum.Current;
                var insertValue = insertEnum.Current;

                if (captureValue.StartTime < insertValue.StartTime)
                {
                    if (!processCommitment(captureValue, result))
                    {
                        return false;
                    }
                    captureHasNext = captureEnum.MoveNext();
                }
                else
                {
                    if (!processCommitment(insertValue, result))
                    {
                        return false;
                    }
                    insertHasNext = insertEnum.MoveNext();
                }
            }

            while (captureHasNext)
            {
                var captureValue = captureEnum.Current;

                if (!processCommitment(captureValue, result))
                {
                    return false;
                }
                captureHasNext = captureEnum.MoveNext();
            }

            while (insertHasNext)
            {
                var insertValue = insertEnum.Current;

                if (!processCommitment(insertValue, result))
                {
                    return false;
                }
                insertHasNext = insertEnum.MoveNext();
            }

            if (currentCommitment != null)
            {
                result.Add(currentCommitment);
            }

            return true;
        }

        [NonAction]
        private bool ProposalSubmitReproducibilityCheck(ScheduleSubmitRequest submitRequest)
        {
            var availabilities = submitRequest.ProposalResponse.ProposalUsers.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime, User = new User() { Email = e.Email } }).ToArray();
            var availabilityDictionary = availabilities.ToDictionaryAggregate(e => e.UserId);

            var userProposals = ScopeStreakToWindow(availabilityDictionary, submitRequest.ProposalRequest.Proposal);

            var respSorted = submitRequest.ProposalResponse.ProposalUsers.OrderBy(e => e.UserId).ThenBy(e => e.StartTime).ThenBy(e => e.EndTime).ToArray();
            var computedSorted = userProposals.OrderBy(e => e.UserId).ThenBy(e => e.StartTime).ThenBy(e => e.EndTime).ToArray();

            if (computedSorted.Length != respSorted.Length)
            {
                return false;
            }

            Func<ScheduleProposalUser, ScheduleProposalUser, bool> availabilitiesEquivalent = (avail1, avail2) =>
            {
                return avail1.UserId == avail2.UserId && avail1.StartTime == avail2.StartTime && avail1.EndTime == avail2.EndTime;
            };

            for (var i = 0; i < respSorted.Length; i++)
            {
                if (!availabilitiesEquivalent(respSorted[i], computedSorted[i]))
                {
                    return false;
                }
            }

            return true;
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
        private bool ProposalSubmitDatabaseCheck(Availability[] dbEntities, ScheduleSubmitRequest submitRequest, out List<Availability>? remove, out List<Availability>? add, out Commitment[]? commitments)
        {
            remove = null;
            add = null;
            commitments = null;

            var availDict = dbEntities.ToDictionaryAggregate(e => e.UserId);

            foreach (var kv in availDict)
            {
                availDict[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }

            var proposalDict = submitRequest.ProposalResponse.ProposalUsers.ToDictionaryAggregate(e => e.UserId);

            foreach (var kv in proposalDict)
            {
                proposalDict[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }

            var removedAvailabilities = new List<Availability>();
            var addedAvailabilities = new List<Availability>();

            Func<Availability, DateTime> availStart = e => e.StartTime;
            Func<Availability, DateTime> availEnd = e => e.EndTime;

            Func<ScheduleProposalUser, DateTime> propStart = e => e.StartTime;
            Func<ScheduleProposalUser, DateTime> propEnd = e => e.EndTime;

            Action<int, DateTime, DateTime> addAvailFunc = (userId, startTime, endTime) =>
            {
                addedAvailabilities.Add(new Availability() { UserId = userId, StartTime = startTime, EndTime = endTime });
            };

            Action<Availability> removeAvailFunc = e =>
            {
                removedAvailabilities.Add(e);
            };

            if (!ProcessAvailabilitiesAndProposals(availDict, availStart, availEnd, proposalDict, propStart, propEnd, addAvailFunc, removeAvailFunc, e => { }))
            {
                return false;
            }

            remove = removedAvailabilities;
            add = addedAvailabilities;
            commitments = proposalDict.SelectMany(e => e.Value).Select(e => new Commitment() { TeamId = submitRequest.ProposalRequest.SelectedTeam.Id, UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime }).ToArray();

            return true;
        }

        [NonAction]
        public static bool ProcessAvailabilitiesAndProposals<T, K>(Dictionary<int, T[]> availabilities, Func<T, DateTime> availStartFunc, Func<T, DateTime> availEndFunc, Dictionary<int, K[]> proposals, Func<K, DateTime> proposalStartFunc, Func<K, DateTime> proposalEndFunc, Action<int, DateTime, DateTime> addAvailability, Action<T> removeAvailability, Action<K> addCommitment)
        {
            foreach (var kv in proposals)
            {
                var userId = kv.Key;

                var availEnum = availabilities[userId].AsEnumerable().GetEnumerator();
                var proposalEnum = proposals[userId].AsEnumerable().GetEnumerator();

                bool availHasNext = false;
                bool proposalHasNext = false;

                DateTime availStart = new DateTime();
                DateTime availEnd = new DateTime();

                DateTime propStart = new DateTime();
                DateTime propEnd = new DateTime();

                Action IncrementAvail = () =>
                {
                    availHasNext = availEnum.MoveNext();
                    if (availHasNext)
                    {
                        availStart = availStartFunc(availEnum.Current);
                        availEnd = availEndFunc(availEnum.Current);
                    }
                };

                Action IncrementProposal = () =>
                {
                    proposalHasNext = proposalEnum.MoveNext();
                    if (proposalHasNext)
                    {
                        propStart = proposalStartFunc(proposalEnum.Current);
                        propEnd = proposalEndFunc(proposalEnum.Current);
                    }
                };

                IncrementAvail();
                IncrementProposal();    

                while (availHasNext && proposalHasNext)
                {
                    var availCurr = availEnum.Current;
                    var proposalCurr = proposalEnum.Current;

                    // Exception
                    if (availStart > propStart)
                    {
                        return false;
                    }

                    // End from available is before propstart
                    if (availEnd <= propStart)
                    {
                        // check for tail 
                        if (availHasNext && availStart != availStartFunc(availEnum.Current) && availStart != availEnd)
                        {
                            addAvailability(userId, availStart, availEnd);
                        }

                        // no collision
                        IncrementAvail();
                        continue;
                    }
                    else // collision
                    {
                        removeAvailability(availEnum.Current);

                        // check for head
                        if (availStart < propStart)
                        {
                            addAvailability(userId, availStart, propStart);
                        }

                        if (availEnd >= propEnd) // avail encapsulated
                        {
                            availStart = propEnd;

                            addCommitment(proposalEnum.Current);    

                            IncrementProposal();
                            continue;
                        }
                        else // avail got first part
                        {
                            propStart = availEnd;

                            IncrementAvail();
                            continue;
                        }
                    }
                }

                if (proposalHasNext)
                {
                    return false;
                }

                // check for tail 
                if (availHasNext && availStart != availStartFunc(availEnum.Current) && availStart != availEnd)
                {
                    addAvailability(userId, availStart, availEnd);
                }
            }

            return true;
        }

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

        /// <summary>
        /// Continuous/Event-based Scoped streak generation.
        /// </summary>
        /// <param name="streaks"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        [NonAction]
        public static HashSet<ScheduleProposalUser> ScopeStreakToWindow(Dictionary<int, Availability[]> streaks, ScheduleProposalAmount[] window)
        {

            var sortedStarts = streaks.Values.SelectMany(e => e).OrderBy(e => e.StartTime);
            window = window.OrderBy(e => e.StartTime).ToArray();

            var endTimesHeap = new DictionaryHeap<Availability>((f, s) => f.EndTime < s.EndTime);
            var usersInHeap = new Dictionary<int, Availability>();
            var usersInHeapButNotSelected = new HashSet<Availability>();

            var usersSelected = new HashSet<ScheduleProposalUser>();
            var userIdsSelected = new Dictionary<int, ScheduleProposalUser>();

            var output = new HashSet<ScheduleProposalUser>();

            var startEnumerator = sortedStarts.GetEnumerator();
            var windowEnumerator = ((IEnumerable<ScheduleProposalAmount>)window).GetEnumerator();

            var startHasNext = startEnumerator.MoveNext();
            var windowHasNext = windowEnumerator.MoveNext();

            DateTime? lastWindowTime = null;

            while (windowHasNext)
            {
                if (windowHasNext && startHasNext)
                {
                    var currStart = startEnumerator.Current;
                    var currWindow = windowEnumerator.Current;

                    if (currStart.StartTime <= currWindow.StartTime)
                    {
                        endTimesHeap.Add(currStart);
                        usersInHeap.Add(currStart.UserId, currStart);
                        usersInHeapButNotSelected.Add(currStart);

                        startHasNext = startEnumerator.MoveNext();
                    }
                    else
                    {
                        ProcessTimeWIndow(currWindow, ref endTimesHeap, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected, ref output);

                        lastWindowTime = currWindow.EndTime;
                        windowHasNext = windowEnumerator.MoveNext();
                    }
                }
                else
                {
                    var currWindow = windowEnumerator.Current;

                    ProcessTimeWIndow(currWindow, ref endTimesHeap, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected, ref output);

                    lastWindowTime = currWindow.EndTime;
                    windowHasNext = windowEnumerator.MoveNext();
                }
            }

            if (lastWindowTime != null)
            {
                foreach (var user in usersSelected)
                {
                    usersSelected.Remove(user);
                    userIdsSelected.Remove(user.UserId);

                    user.EndTime = (DateTime)lastWindowTime;

                    output.Add(user);
                }
            }

            return output;
        }

        [NonAction]
        public static void ProcessTimeWIndow(ScheduleProposalAmount currWindow, ref DictionaryHeap<Availability> endTimes, ref Dictionary<int, Availability> usersInHeap, ref HashSet<Availability> usersInHeapButNotSelected, ref HashSet<ScheduleProposalUser> usersSelected, ref Dictionary<int, ScheduleProposalUser> userIdsSelected, ref HashSet<ScheduleProposalUser> output)
        {
            var currentTime = currWindow.StartTime;

            // remove old
            while (!endTimes.IsEmpty() && endTimes.Peek().EndTime < currWindow.EndTime)
            {
                // clean heap
                var popedUser = endTimes.Pop();
                usersInHeap.Remove(popedUser.UserId);
                usersInHeapButNotSelected.Remove(popedUser);

                if (userIdsSelected.ContainsKey(popedUser.UserId))
                {
                    var remove = userIdsSelected[popedUser.UserId];
                    usersSelected.Remove(userIdsSelected[popedUser.UserId]);
                    userIdsSelected.Remove(popedUser.UserId);

                    remove.EndTime = popedUser.EndTime;

                    output.Add(remove);
                }

                // remove excess (should only happen first iteration)
                RemoveExcessFromWindow(currWindow, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected, ref output);

                // on change (don't prematurely add)
                if (currentTime != popedUser.EndTime)
                {
                    AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected);
                }

                currentTime = popedUser.EndTime;
            }
            // last add
            AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected);

            // remove excess (if while loop did not enter)
            RemoveExcessFromWindow(currWindow, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected, ref output);
        }

        [NonAction]
        private static void RemoveExcessFromWindow(ScheduleProposalAmount currWindow, ref Dictionary<int, Availability> usersInHeap, ref HashSet<Availability> usersInHeapButNotSelected, ref HashSet<ScheduleProposalUser> usersSelected, ref Dictionary<int, ScheduleProposalUser> userIdsSelected, ref HashSet<ScheduleProposalUser> output)
        {
            // remove excess (should only happen first iteration)
            if (usersSelected.Count > currWindow.Employees)
            {
                while (usersSelected.Count > currWindow.Employees)
                {
                    var random = usersSelected.First();
                    usersSelected.Remove(random);
                    userIdsSelected.Remove(random.UserId);

                    var avail = usersInHeap[random.UserId];
                    usersInHeapButNotSelected.Add(avail);

                    random.EndTime = currWindow.StartTime;

                    output.Add(random);
                }
            }
        }

        [NonAction]
        public static void AddUsersFromHeap(ScheduleProposalAmount currWindow, DateTime currentTime, ref Dictionary<int, Availability> usersInHeap, ref HashSet<Availability> usersInHeapButNotSelected, ref HashSet<ScheduleProposalUser> usersSelected, ref Dictionary<int, ScheduleProposalUser> userIdsSelected)
        {
            if (usersSelected.Count < currWindow.Employees && usersSelected.Count < usersInHeap.Count) // less
            {
                // add
                var amountToAdd = currWindow.Employees - usersSelected.Count;

                while (amountToAdd > 0 && usersInHeapButNotSelected.Count != 0)
                {
                    var random = usersInHeapButNotSelected.First();
                    usersInHeapButNotSelected.Remove(random);

                    var response = new ScheduleProposalUser()
                    {
                        Email = random.User.Email,
                        StartTime = currentTime,
                        EndTime = random.EndTime,
                        UserId = random.UserId,
                    };

                    usersSelected.Add(response);
                    userIdsSelected.Add(response.UserId, response);

                    amountToAdd--;
                }
            }
        }
    }
}
