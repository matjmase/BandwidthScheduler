using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Controllers;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BandwidthScheduler.Server.Test.PublishControllerTest;

namespace BandwidthScheduler.Server.Test
{
    public class AvailabilityControllerTest
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

            foreach (var kv in _applicabilities)
            {
                _applicabilities[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }
        }

        [Test]
        public void TestStreaks()
        {

            for (var i = 0; i < _numberOfUsers; i++)
            {
                if (!TimeFrameFunctions.CreateStreaksAvailability(_applicabilities[i], out var streaks) || streaks == null)
                {
                    Assert.Fail();
                }
                else
                {
                    var totalStreak = Math.Ceiling((_endTime - _startTime).TotalMinutes / _timeDiffMinutes / (i + 1));
                    if (!(i == 0 && (streaks.Count == 1) || totalStreak == streaks.Count))
                    {
                        Assert.Fail();
                    }
                }
            }
        }
    }
}
