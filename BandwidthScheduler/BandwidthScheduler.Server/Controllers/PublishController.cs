using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Common.Role;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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
            var totalAvailabilities = await GetAvailabilities(proposalRequest);

            if (totalAvailabilities == null)
            {
                return BadRequest("Invalid Proposal");
            }

            var userAvailabilityArrays = totalAvailabilities.ToDictionaryAggregate(e => e.UserId);

            var streaks = CreateStreaks(userAvailabilityArrays);

            var output = ScopeStreakToWindow(streaks, proposalRequest.Proposal);

            return Ok(new ScheduleProposalResponse() { ProposalUsers = output.ToArray() });
        }


        [HttpPost("submit")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> ProposalSubmit([FromBody] ScheduleSubmitRequest submitRequest)
        {
            /// Reproducibility checking

            if (!ProposalSubmitReproducibilityCheck(submitRequest))
            {
                return BadRequest("Request and Response data has been altered");
            }

            /// DB Checking.

            var totalApplicable = await GetAvailabilities(submitRequest.ProposalRequest);

            if (totalApplicable == null)
            {
                return BadRequest("Invalid Proposal");
            }

            if (!ProposalSubmitDatabaseCheck(totalApplicable, submitRequest, out var removedAvailabilities, out var addedAvailabilities, out var proposalDict))
            {
                return BadRequest("Availabilites have changed");
            }

            // validation done, submit to DB

            _db.Availabilities.RemoveRange(_db.Availabilities.Where(e => removedAvailabilities.Select(a => a.Id).Contains(e.Id)));
            await _db.Availabilities.AddRangeAsync(addedAvailabilities);
            await _db.Commitments.AddRangeAsync(proposalDict);

            await _db.SaveChangesAsync();

            return Ok();
        }

        [NonAction]
        private bool ProposalSubmitReproducibilityCheck(ScheduleSubmitRequest submitRequest)
        {
            var availabilities = submitRequest.ProposalResponse.ProposalUsers.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime, User = new User() { Email = e.Email } }).ToArray();
            var availabilityDictionary = availabilities.ToDictionaryAggregate(e => e.UserId).SelectDictionaryValue(v => v.ToList());

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
        private bool ProposalSubmitDatabaseCheck(Availability[] dbEntities, ScheduleSubmitRequest submitRequest, out List<Availability> remove, out List<Availability> add, out Commitment[] commitments)
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

            // Not doing scoping so they will be different.

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
            foreach (var kv in availabilities)
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
        public async Task<Availability[]?> GetAvailabilities(ScheduleProposalRequest request)
        {
            if (request == null || request.Proposal.Length == 0 || request.SelectedTeam == null)
            {
                return null;
            }

            var sorted = request.Proposal.OrderBy(e => e.StartTime).ToArray();

            var start = sorted[0].StartTime.ToUniversalTime();
            var end = sorted[sorted.Length - 1].EndTime.ToUniversalTime();

            var totalApplicable = await _db.UserRoles
                .Where(e => e.RoleId == (int)AuthenticationRole.User) // role filtering
                .Include(e => e.User).ThenInclude(e => e.UserTeams) // userteam include
                .Where(e => e.User.UserTeams.Any(e => e.TeamId == request.SelectedTeam.Id)) // userteam filter
                .Include(e => e.User).ThenInclude(e => e.Availabilities).ThenInclude(e => e.User) // availabilities include with user
                .Select(e => e.User).SelectMany(e => e.Availabilities) // availabilies nav
                .Where(e => e.StartTime >= start && e.EndTime <= end).OrderBy(e => e.StartTime).ToArrayAsync(); // availability filter
            totalApplicable = totalApplicable.Select(e =>
            new Availability()
            {
                Id = e.Id,
                UserId = e.UserId,
                User = e.User,
                StartTime = DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc),
                EndTime = DateTime.SpecifyKind(e.EndTime, DateTimeKind.Utc),
            }).ToArray();

            return totalApplicable;
        }

        /// <summary>
        /// Make the events continuous to "streaks"
        /// </summary>
        /// <param name="segmentedAvailability"></param>
        /// <returns></returns>
        [NonAction]
        public static Dictionary<int, List<Availability>> CreateStreaks(Dictionary<int, Availability[]> segmentedAvailability)
        {
            var streaks = new Dictionary<int, List<Availability>>();
            foreach (var userId in segmentedAvailability.Keys)
            {
                var sorted = segmentedAvailability[userId].OrderBy(e => e.StartTime);

                streaks.Add(userId, new List<Availability>());

                Availability? lastApplicable = null;
                foreach (var applicability in sorted)
                {
                    if (lastApplicable == null)
                    {
                        lastApplicable = applicability;
                    }
                    else
                    {
                        if (lastApplicable.EndTime == applicability.StartTime)
                        {
                            lastApplicable.EndTime = applicability.EndTime;
                        }
                        else
                        {
                            streaks[userId].Add(lastApplicable);
                            lastApplicable = applicability;
                        }
                    }
                }

                if (lastApplicable != null)
                {
                    streaks[userId].Add(lastApplicable);
                }
            }

            return streaks;
        }

        /// <summary>
        /// Continuous/Event-based Scoped streak generation.
        /// </summary>
        /// <param name="streaks"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        [NonAction]
        public static HashSet<ScheduleProposalUser> ScopeStreakToWindow(Dictionary<int, List<Availability>> streaks, ScheduleProposalAmount[] window)
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
