using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

            var userAvailabilityArrays = AvailabilitiesToDictionary(totalAvailabilities);

            var streaks = CreateStreaks(userAvailabilityArrays);

            var output = ScopeStreakToWindow(streaks, proposalRequest.Proposal);

            return Ok(new ScheduleProposalResponse() { ProposalUsers = output.ToArray() });
        }


        [HttpPost("submit")]
        [Authorize(Roles = "Scheduler")]
        public async Task<IActionResult> ProposalSubmit([FromBody] ScheduleSubmitRequest submitRequest)
        {
            var totalApplicable = await GetAvailabilities(submitRequest.ProposalRequest);

            if (totalApplicable == null)
            {
                return BadRequest("Invalid Proposal");
            }

            return Ok();
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

            var totalApplicable = await _db.Teams.Where(e => e.Id == request.SelectedTeam.Id).Include(e => e.UserTeams).ThenInclude(e => e.User).ThenInclude(e => e.Availabilities).ThenInclude(e => e.User).SelectMany(e => e.UserTeams).Select(e => e.User).SelectMany(e => e.Availabilities).Where(e => e.StartTime >= start && e.EndTime <= end).OrderBy(e => e.StartTime).ToArrayAsync();
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

        [NonAction]
        public static Dictionary<int, Availability[]> AvailabilitiesToDictionary(Availability[] totalAvailabilities)
        {
            var userAvailabilityCounts = new Dictionary<int, int>();

            foreach (var availability in totalAvailabilities)
            {
                if (!userAvailabilityCounts.ContainsKey(availability.UserId))
                {
                    userAvailabilityCounts.Add(availability.UserId, 0);
                }

                userAvailabilityCounts[availability.UserId]++;
            }

            var userAvailabilityArrays = new Dictionary<int, Availability[]>();

            foreach (var user in userAvailabilityCounts)
            {
                userAvailabilityArrays.Add(user.Key, new Availability[user.Value]);
                userAvailabilityCounts[user.Key] = 0;
            }

            foreach (var availability in totalAvailabilities)
            {
                userAvailabilityArrays[availability.UserId][userAvailabilityCounts[availability.UserId]] = availability;
                userAvailabilityCounts[availability.UserId]++;
            }

            return userAvailabilityArrays;
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
            // remove excess
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

                // on change (don't prematurely add)
                if (currentTime != popedUser.EndTime)
                {
                    AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected);
                }

                currentTime = popedUser.EndTime;
            }

            // last add
            AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersInHeapButNotSelected, ref usersSelected, ref userIdsSelected);
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
