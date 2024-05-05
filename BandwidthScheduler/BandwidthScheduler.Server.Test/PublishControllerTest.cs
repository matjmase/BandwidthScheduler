using BandwidthScheduler.Server.Common.DataStructures;
using BandwidthScheduler.Server.Controllers;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;

namespace BandwidthScheduler.Server.Test
{
    public class PublishControllerTest
    {
        private int _timeDiffMinutes;

        private DateTime _startTime;
        private DateTime _endTime;

        private int _totalEmployees;

        private ScheduleProposal[] _schedule;

        private int _numberOfUsers = 10;

        private Dictionary<int, List<Availability>> _applicabilities;

        [SetUp]
        public void Setup()
        {
            _timeDiffMinutes = 30;

            _startTime = new DateTime(2024, 4, 7);
            _endTime = new DateTime(2024, 4, 10);

            _totalEmployees = 7;

            var schedule = new List<ScheduleProposal>(); 
            for (var i = _startTime; i < _endTime; i = i.AddMinutes(_timeDiffMinutes))
            {
                schedule.Add(new ScheduleProposal()
                {
                    StartTime = i,
                    EndTime = i.AddMinutes(_timeDiffMinutes),
                    Employees = _totalEmployees
                });
            }

            _schedule = schedule.ToArray();

            var applicabilities = new List<UserApplicabilityTestingModel>();
            for (var i = _startTime; i < _endTime; i = i.AddMinutes(_timeDiffMinutes))
            {
                schedule.Add(new ScheduleProposal()
                {
                    StartTime = i,
                    EndTime = i.AddMinutes(_timeDiffMinutes),
                    Employees = _totalEmployees
                });
            }

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
            });

            var segemented = convert.Aggregate(new Dictionary<int, List<Availability>>(), (s, i) =>
            {
                if (!s.ContainsKey(i.UserId))
                {
                    s.Add(i.UserId, new List<Availability>());
                }

                s[i.UserId].Add(i);

                return s;
            });

            _applicabilities = segemented;   
        }

        [Test]
        public void TestStreaks()
        {
            var streaks = PublishController.CreateStreaks(_applicabilities);

            for (var i = 0; i < _numberOfUsers; i++)
            {
                var totalStreak = Math.Ceiling((_endTime - _startTime).TotalMinutes / _timeDiffMinutes / (i + 1));
                if (!(i == 0 && (streaks[i].Count == 1) || totalStreak == streaks[i].Count))
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestStreakScoping()
        {
            var streaks = PublishController.CreateStreaks(_applicabilities);
            var scoped = PublishController.ScopeStreakToWindow(streaks, _schedule);

            var currHeap = new Heap<ScheduleProposalResponse>((f, s) => f.EndTime < s.EndTime);

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

            Action<ScheduleProposalResponse> incrementScoped = (ScheduleProposalResponse resp) =>
            {
                currHeap.Add(resp);
                scopedHasNext = scopedEnum.MoveNext();
            };

            Action<ScheduleProposal> incrementWindow = (ScheduleProposal req) =>
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

        public class UserApplicabilityTestingModel
        {
            public int UserId { get; set; }
            public string Email { get; set; }   
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}