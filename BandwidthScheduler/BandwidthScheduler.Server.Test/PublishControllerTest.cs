using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Controllers;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;
using Microsoft.AspNetCore.Mvc;

namespace BandwidthScheduler.Server.Test
{
    public class PublishControllerTest
    {
        private int _timeDiffMinutes;

        private DateTime _startTime;
        private DateTime _endTime;

        private int _totalEmployees;

        private ScheduleProposalAmount[] _schedule;

        private int _numberOfUsers = 10;

        private Dictionary<int, Availability[]> _applicabilities;

        [SetUp]
        public void Setup()
        {
            _timeDiffMinutes = 30;

            _startTime = new DateTime(2024, 4, 7);
            _endTime = new DateTime(2024, 4, 10);

            _totalEmployees = 7;

            var schedule = new List<ScheduleProposalAmount>(); 
            for (var i = _startTime; i < _endTime; i = i.AddMinutes(_timeDiffMinutes))
            {
                schedule.Add(new ScheduleProposalAmount()
                {
                    StartTime = i,
                    EndTime = i.AddMinutes(_timeDiffMinutes),
                    Employees = _totalEmployees
                });
            }

            _schedule = schedule.ToArray();

            var applicabilities = new List<UserApplicabilityTestingModel>();

            for (var i = 0; i < _numberOfUsers; i++) 
            {
                for (var m = 0; _startTime.AddMinutes(m) < _endTime; m += _timeDiffMinutes)
                {
                    if ((m / _timeDiffMinutes) % (i + 1) == 0)
                    {
                        applicabilities.Add(new UserApplicabilityTestingModel()
                        {
                            UserId = i,
                            Email = ((char)('a' + i)).ToString(),
                            StartTime = _startTime.AddMinutes(m),
                            EndTime = _startTime.AddMinutes(m).AddMinutes(_timeDiffMinutes),
                        });
                    }
                }
            }

            var convert = applicabilities.Select(e => new Availability()
            {
                UserId = e.UserId,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                User = new User()
                {
                    Id = e.UserId,
                    Email = e.Email,
                }
            }).ToArray();

            _applicabilities = convert.ToDictionaryAggregate(e => e.UserId);   

            foreach(var kv in _applicabilities) 
            {
                _applicabilities[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }
        }

        /// <summary>
        /// Make the events continuous to "streaks"
        /// </summary>
        /// <param name="segmentedAvailability"></param>
        /// <returns></returns>
        [NonAction]
        public static Dictionary<int, List<Availability>> CreateStreaksForAll(Dictionary<int, Availability[]> segmentedAvailability)
        {
            var userStreaks = new Dictionary<int, List<Availability>>();

            foreach (var userId in segmentedAvailability.Keys)
            {
                if (!TimeFrameFunctions.CreateStreaksAvailability(segmentedAvailability[userId], out var streaks) || streaks == null)
                {
                    Assert.Fail();
                }
                else
                {
                    userStreaks.Add(userId, streaks);
                }
            }

            return userStreaks;
        }

        /// <summary>
        /// Designed for time intervals as opposed to the continuous/event based nature of the algorithm in production.
        /// </summary>
        [Test]
        public void TestStreakScoping()
        {
            var streaks = CreateStreaksForAll(_applicabilities); // All Db will be continuous

            var scoped = PublishController.ScopeStreakToWindow(streaks.SelectDictionaryValue(e => e.ToArray()), _schedule);

            var currHeap = new Heap<ScheduleProposalUser>((f, s) => f.EndTime < s.EndTime);

            var scopedEnum = scoped.OrderBy(e => e.StartTime).GetEnumerator();
            var windowEnum = _schedule.OrderBy(e => e.StartTime).GetEnumerator();

            var scopedHasNext = scopedEnum.MoveNext();
            var windowHasNext = windowEnum.MoveNext();

            var applicabilitiesDict = _applicabilities.Values.SelectMany(e => e).Aggregate(new Dictionary<DateTime, HashSet<Availability>>(), (s, i) =>
            {
                if (!s.ContainsKey(i.StartTime))
                {
                    s.Add(i.StartTime, new HashSet<Availability>());
                }

                s[i.StartTime].Add(i);

                return s;
            });

            var scheduleDict = _schedule.ToDictionary(e => e.StartTime);

            Action<ScheduleProposalUser> incrementScoped = (ScheduleProposalUser resp) =>
            {
                currHeap.Add(resp);
                scopedHasNext = scopedEnum.MoveNext();
            };

            Action<ScheduleProposalAmount> incrementWindow = (ScheduleProposalAmount req) =>
            {
                // Update heap
                while (!currHeap.IsEmpty() && currHeap.Peek().EndTime < req.EndTime)
                {
                    currHeap.Pop();
                }

                if (Math.Min(applicabilitiesDict[req.StartTime].Count, scheduleDict[req.StartTime].Employees) != currHeap.Count())
                {
                    Assert.Fail();  
                }

                windowHasNext = windowEnum.MoveNext();
            };

            while (scopedHasNext || windowHasNext)
            {
                if (scopedHasNext && windowHasNext)
                {
                    if (scopedEnum.Current.StartTime <= windowEnum.Current.StartTime)
                    {
                        incrementScoped(scopedEnum.Current);
                    }
                    else
                    {
                        incrementWindow(windowEnum.Current);
                    }
                }
                else if (windowHasNext)
                {
                    incrementWindow(windowEnum.Current);
                }
            }
        }

        [Test]
        public void TestProcessAvailabilitiesAndProposalsIdentity()
        {
            Func<Availability, DateTime> start = e => e.StartTime;
            Func<Availability, DateTime> end = e => e.EndTime;

            var addCommitment = new List<Availability>();
            var removeAvailability = new List<Availability>();
            var addAvailability = new List<Availability>();

            Action<int, DateTime, DateTime> addAvailabilityFunc = (userId, start, end) => { addAvailability.Add(new Availability() { UserId = userId, StartTime = start, EndTime = end }); };
            Action<Availability> removeAvailabilityFunc = e => { removeAvailability.Add(e); };
            Action<Availability> addCommitmentFunc = e => { addCommitment.Add(e); };

            if (!PublishController.ProcessAvailabilitiesAndProposals(_applicabilities, start, end, _applicabilities, start, end, addAvailabilityFunc, removeAvailabilityFunc, addCommitmentFunc))
            {
                Assert.Fail();
            }

            var commitmentDict = addCommitment.ToDictionaryAggregate(e => e.UserId);
            var removeDict = removeAvailability.ToDictionaryAggregate(e => e.UserId);
            var addDict = addAvailability.ToDictionaryAggregate(e => e.UserId);
            
            if (addDict.Count() != 0)
            {
                Assert.Fail();
            }

            Func<Availability, Availability, bool> availabilitiesAreEqual = (f, s) =>
            {
                return f.UserId == s.UserId && f.StartTime == s.StartTime && f.EndTime == s.EndTime;
            };

            foreach (var userId in _applicabilities.Keys)
            {
                if (_applicabilities[userId].Length != commitmentDict[userId].Length || commitmentDict[userId].Length != removeDict[userId].Length)
                {
                    Assert.Fail();
                }

                for(var i = 0; i < _applicabilities[userId].Length; i++)
                {
                    var areEqual = true;
                    areEqual = areEqual && availabilitiesAreEqual(_applicabilities[userId][i], commitmentDict[userId][i]);
                    areEqual = areEqual && availabilitiesAreEqual(commitmentDict[userId][i], removeDict[userId][i]);

                    if (!areEqual)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        [Test]
        public void TestProcessAvailabilitiesAndProposalsShift()
        {
            Func<Availability, DateTime> start = e => e.StartTime;
            Func<Availability, DateTime> end = e => e.EndTime;

            var addCommitment = new List<Availability>();
            var removeAvailability = new List<Availability>();
            var addAvailability = new List<Availability>();

            Action<int, DateTime, DateTime> addAvailabilityFunc = (userId, start, end) => { addAvailability.Add(new Availability() { UserId = userId, StartTime = start, EndTime = end }); };
            Action<Availability> removeAvailabilityFunc = e => { removeAvailability.Add(e); };
            Action<Availability> addCommitmentFunc = e => { addCommitment.Add(e); };

            var shifted = new Dictionary<int, Availability[]>();

            foreach (var applicable in _applicabilities)
            {
                shifted.Add(applicable.Key, applicable.Value.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime.AddMinutes(_timeDiffMinutes), EndTime = e.EndTime.AddMinutes(_timeDiffMinutes) }).ToArray());
            }

            if (PublishController.ProcessAvailabilitiesAndProposals(_applicabilities, start, end, shifted, start, end, addAvailabilityFunc, removeAvailabilityFunc, addCommitmentFunc))
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TestProcessAvailabilitiesAndProposalsShrink()
        {
            Func<Availability, DateTime> start = e => e.StartTime;
            Func<Availability, DateTime> end = e => e.EndTime;

            var addCommitment = new List<Availability>();
            var removeAvailability = new List<Availability>();
            var addAvailability = new List<Availability>();

            Action<int, DateTime, DateTime> addAvailabilityFunc = (userId, start, end) => { addAvailability.Add(new Availability() { UserId = userId, StartTime = start, EndTime = end }); };
            Action<Availability> removeAvailabilityFunc = e => { removeAvailability.Add(e); };
            Action<Availability> addCommitmentFunc = e => { addCommitment.Add(e); };

            var shrink = new Dictionary<int, Availability[]>();

            var diff = new TimeSpan(0, _timeDiffMinutes, 0);
            var diffTicks = diff.Ticks;
            var bothSides = diffTicks / 2; 
            var halfLength = bothSides / 2; 

            foreach (var applicable in _applicabilities)
            {
                shrink.Add(applicable.Key, applicable.Value.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime.AddTicks(halfLength), EndTime = e.EndTime.AddTicks(-halfLength) }).ToArray());
            }

            if (!PublishController.ProcessAvailabilitiesAndProposals(_applicabilities, start, end, shrink, start, end, addAvailabilityFunc, removeAvailabilityFunc, addCommitmentFunc))
            {
                Assert.Fail();
            }

            var commitmentDict = addCommitment.ToDictionaryAggregate(e => e.UserId);
            var removeDict = removeAvailability.ToDictionaryAggregate(e => e.UserId);
            var addDict = addAvailability.ToDictionaryAggregate(e => e.UserId);

            foreach (var userId in _applicabilities.Keys)
            {
                if (_applicabilities[userId].Length != commitmentDict[userId].Length || commitmentDict[userId].Length * 2 != addDict[userId].Length)
                {
                    Assert.Fail();
                }
            }
        }

        public class UserApplicabilityTestingModel
        {
            public int UserId { get; set; }
            public string Email { get; set; }   
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}