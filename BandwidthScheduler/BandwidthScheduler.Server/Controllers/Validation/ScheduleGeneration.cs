using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;
using Microsoft.AspNetCore.Mvc;

namespace BandwidthScheduler.Server.Controllers.Validation
{
    public static class ScheduleGeneration
    {
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
