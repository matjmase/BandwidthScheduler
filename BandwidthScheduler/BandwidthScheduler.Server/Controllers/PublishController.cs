using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models;
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
            if(proposalRequest == null || proposalRequest.Proposal.Length == 0 || proposalRequest.SelectedTeam == null)
            {
                return BadRequest("Invalid Proposal");
            }

            var start = proposalRequest.Proposal[0].StartTime.ToUniversalTime();
            var end = proposalRequest.Proposal[proposalRequest.Proposal.Length - 1].EndTime.ToUniversalTime();

            var totalApplicable = await _db.Teams.Where(e => e.Id == proposalRequest.SelectedTeam.Id).Include(e => e.UserTeams).ThenInclude(e => e.User).ThenInclude(e => e.Availabilities).ThenInclude(e => e.User).SelectMany(e => e.UserTeams).Select(e => e.User).SelectMany(e => e.Availabilities).Where(e => e.StartTime >= start && e.EndTime <= end).OrderBy(e => e.StartTime).ToArrayAsync();
            totalApplicable = totalApplicable.Select(e =>
            new Availability()
            {
                Id = e.Id,
                UserId = e.UserId,
                User = e.User,
                StartTime = DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc),
                EndTime = DateTime.SpecifyKind(e.EndTime, DateTimeKind.Utc),
            }).ToArray();

            var applicableUsers = new Dictionary<int, List<Availability>>();
            foreach (var applicability in totalApplicable)
            { 
                if(!applicableUsers.ContainsKey(applicability.UserId))
                {
                    applicableUsers.Add(applicability.UserId, new List<Availability>());
                }

                applicableUsers[applicability.UserId].Add(applicability);
            }

            var streaks = CreateStreaks(applicableUsers);

            var output = ScopeStreakToWindow(streaks, proposalRequest.Proposal);

            return Ok(output);
        }

        /// <summary>
        /// Make the events continuous to "streaks"
        /// </summary>
        /// <param name="segmentedAvailability"></param>
        /// <returns></returns>
        [NonAction]
        public static Dictionary<int, List<Availability>> CreateStreaks(Dictionary<int, List<Availability>> segmentedAvailability)
        {
            var streaks = new Dictionary<int, List<Availability>>();
            foreach (var userId in segmentedAvailability.Keys)
            {
                streaks.Add(userId, new List<Availability>());

                Availability? lastApplicable = null;
                foreach (var applicability in segmentedAvailability[userId])
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
        public static HashSet<ScheduleProposalResponse> ScopeStreakToWindow(Dictionary<int, List<Availability>> streaks, ScheduleProposal[] window)
        {

            var sortedStarts = streaks.Values.SelectMany(e => e).OrderBy(e => e.StartTime);

            var endTimesHeap = new DictionaryHeap<Availability>((f, s) => f.EndTime < s.EndTime);
            var usersInHeap = new HashSet<Availability>();

            var usersSelected = new HashSet<ScheduleProposalResponse>();
            var userIdsSelected = new Dictionary<int, ScheduleProposalResponse>();

            var output = new HashSet<ScheduleProposalResponse>();

            var startEnumerator = sortedStarts.GetEnumerator();
            var windowEnumerator = ((IEnumerable<ScheduleProposal>)window).GetEnumerator();

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
                        usersInHeap.Add(currStart);

                        startHasNext = startEnumerator.MoveNext();
                    }
                    else
                    {
                        ProcessTimeWIndow(currWindow, ref endTimesHeap, ref usersInHeap, ref usersSelected, ref userIdsSelected, ref output);

                        lastWindowTime = currWindow.EndTime;
                        windowHasNext = windowEnumerator.MoveNext();
                    }
                }
                else
                {
                    var currWindow = windowEnumerator.Current;

                    ProcessTimeWIndow(currWindow, ref endTimesHeap, ref usersInHeap, ref usersSelected, ref userIdsSelected, ref output);

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
        public static void ProcessTimeWIndow(ScheduleProposal currWindow, ref DictionaryHeap<Availability> endTimes, ref HashSet<Availability> usersInHeap, ref HashSet<ScheduleProposalResponse> usersSelected, ref Dictionary<int, ScheduleProposalResponse> userIdsSelected, ref HashSet<ScheduleProposalResponse> output)
        {
            // remove excess
            if (usersSelected.Count > currWindow.Employees)
            {
                while (usersSelected.Count > currWindow.Employees)
                {
                    var random = usersSelected.First();
                    usersSelected.Remove(random);
                    userIdsSelected.Remove(random.UserId);

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
                usersInHeap.Remove(popedUser);
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
                    AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersSelected, ref userIdsSelected);
                }

                currentTime = popedUser.EndTime;
            }

            // last add
            AddUsersFromHeap(currWindow, currentTime, ref usersInHeap, ref usersSelected, ref userIdsSelected);
        }

        [NonAction]
        public static void AddUsersFromHeap(ScheduleProposal currWindow, DateTime currentTime, ref HashSet<Availability> usersInHeap, ref HashSet<ScheduleProposalResponse> usersSelected, ref Dictionary<int, ScheduleProposalResponse> userIdsSelected)
        {
            if (usersSelected.Count < currWindow.Employees && usersSelected.Count < usersInHeap.Count) // less
            {
                // add
                var amountToAdd = currWindow.Employees - usersSelected.Count;

                var toAddList = new List<Availability>();

                foreach (var user in usersInHeap)
                {
                    if (!userIdsSelected.ContainsKey(user.UserId))
                    {
                        toAddList.Add(user);
                    }
                }

                var toAdd = toAddList.ToArray();

                for (var i = 0; i < amountToAdd && i < toAdd.Length; i++)
                {
                    var response = new ScheduleProposalResponse()
                    {
                        Email = toAdd[i].User.Email,
                        StartTime = currentTime,
                        EndTime = toAdd[i].EndTime,
                        UserId = toAdd[i].UserId,
                    };

                    usersSelected.Add(response);
                    userIdsSelected.Add(response.UserId, response);
                }
            }
        }
    }
}
