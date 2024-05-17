using BandwidthScheduler.Server.Controllers;
using BandwidthScheduler.Server.DbModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BandwidthScheduler.Server.Test.PublishControllerTest;

namespace BandwidthScheduler.Server.Test
{
    public class DbQueryTest
    {
        private const int _userId = 0;

        private TimeWindowTestingModel _mainWindow;

        private HashSet<TimeWindowTestingModel> _timeWindowEncapsulated;
        private HashSet<TimeWindowTestingModel> _leftNeighbor;
        private HashSet<TimeWindowTestingModel> _rightNeighbor;
        private HashSet<TimeWindowTestingModel> _timeWindowsCaptured;

        private HashSet<TimeWindowTestingModel> _noIntersection;
        private HashSet<TimeWindowTestingModel> _intersection;

        private readonly Func<TimeWindowTestingModel, Availability> _availabilityTransformation = tw => new Availability() { UserId = _userId, StartTime = tw.StartTime, EndTime = tw.EndTime };
        private readonly Func<TimeWindowTestingModel, Commitment> _commitmentTransformation = tw => new Commitment() { UserId = _userId, StartTime = tw.StartTime, EndTime = tw.EndTime };



        [SetUp]
        public void Setup()
        {
            _mainWindow = new TimeWindowTestingModel()
            {
                StartTime = new DateTime(2024, 10, 5),
                EndTime = new DateTime(2024, 10, 10),
            };

            _timeWindowsCaptured = new HashSet<TimeWindowTestingModel>();
            var numberOfUser = 10;

            var timeDiffMinutes = 30;

            for (var i = 0; i < numberOfUser; i++)
            {
                for (var m = 0; _mainWindow.StartTime.AddMinutes(m) < _mainWindow.EndTime; m += timeDiffMinutes)
                {
                    if ((m / timeDiffMinutes) % (i + 1) == 0)
                    {
                        _timeWindowsCaptured.Add(new TimeWindowTestingModel()
                        {
                            StartTime = _mainWindow.StartTime.AddMinutes(m),
                            EndTime = _mainWindow.StartTime.AddMinutes(m).AddMinutes(timeDiffMinutes),
                        });
                    }
                }
            }

            _leftNeighbor = new HashSet<TimeWindowTestingModel>();

            _leftNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime.AddMinutes(-timeDiffMinutes), EndTime = _mainWindow.StartTime });  
            _leftNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime.AddMinutes(-timeDiffMinutes), EndTime = _mainWindow.StartTime.AddMinutes(timeDiffMinutes) });
            _leftNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime.AddMinutes(-timeDiffMinutes), EndTime = _mainWindow.EndTime });
        
            _rightNeighbor = new HashSet<TimeWindowTestingModel>();    

            _rightNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.EndTime, EndTime = _mainWindow.EndTime.AddMinutes(timeDiffMinutes) });
            _rightNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.EndTime.AddMinutes(-timeDiffMinutes), EndTime = _mainWindow.EndTime.AddMinutes(timeDiffMinutes) });
            _rightNeighbor.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime, EndTime = _mainWindow.EndTime.AddMinutes(timeDiffMinutes) });

            _timeWindowEncapsulated = new HashSet<TimeWindowTestingModel>();

            _timeWindowEncapsulated.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime.AddMinutes(-timeDiffMinutes), EndTime = _mainWindow.EndTime.AddMinutes(timeDiffMinutes) });

            _noIntersection = new HashSet<TimeWindowTestingModel>();

            _noIntersection.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.StartTime.AddMinutes(-2 * timeDiffMinutes), EndTime = _mainWindow.StartTime.AddMinutes(-timeDiffMinutes) });
            _noIntersection.Add(new TimeWindowTestingModel() { StartTime = _mainWindow.EndTime.AddMinutes(timeDiffMinutes), EndTime = _mainWindow.EndTime.AddMinutes(2 * timeDiffMinutes) });
            
            _intersection = [.. _timeWindowEncapsulated, .. _leftNeighbor, .. _rightNeighbor, .. _timeWindowsCaptured];
        }

        [Test]
        public void Encapsulated()
        {
            var availQuery = AvailabilityController.AvailabilityTimeWindowEncapsulatedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            foreach (var item in _timeWindowEncapsulated)
            {
                if (!availQuery(_availabilityTransformation(item)))
                { 
                    Assert.Fail();  
                }
            }
        }

        [Test]
        public void LeftNeighbor()
        {
            var availQuery = AvailabilityController.AvailabilityLeftNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            foreach (var item in _leftNeighbor)
            {
                if (!availQuery(_availabilityTransformation(item)))
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void RightNeighbor()
        {
            var availQuery = AvailabilityController.AvailabilityRightNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            foreach (var item in _rightNeighbor)
            {
                if (!availQuery(_availabilityTransformation(item)))
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void Captured()
        {
            var availQuery = AvailabilityController.AvailabilityTimeWindowsCapturedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            foreach (var item in _timeWindowsCaptured)
            {
                if (!availQuery(_availabilityTransformation(item)))
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void MutuallyExclusive()
        {
            var encapsulated = AvailabilityController.AvailabilityTimeWindowEncapsulatedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var left = AvailabilityController.AvailabilityLeftNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var right = AvailabilityController.AvailabilityRightNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var captured = AvailabilityController.AvailabilityTimeWindowsCapturedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            var functionsAndCollections = new List<Tuple<string, Func<Availability, bool>, HashSet<TimeWindowTestingModel>>>
            {
                new Tuple<string, Func<Availability, bool>, HashSet<TimeWindowTestingModel>>("encapsulated", encapsulated, _timeWindowEncapsulated),
                new Tuple<string, Func<Availability, bool>, HashSet<TimeWindowTestingModel>>("left", left, _leftNeighbor),
                new Tuple<string, Func<Availability, bool>, HashSet<TimeWindowTestingModel>>("right", right, _rightNeighbor),
                new Tuple<string, Func<Availability, bool>, HashSet<TimeWindowTestingModel>>("captured", captured, _timeWindowsCaptured)
            };

            foreach (var pair in functionsAndCollections)
            {
                foreach (var item in _intersection)
                {
                    if (!pair.Item3.Contains(item) && pair.Item2(_availabilityTransformation(item)))
                    { 
                        Assert.Fail($"false Positive for ${ pair.Item1 } for collection ${ functionsAndCollections.First(e => e.Item3.Contains(item)).Item1 }");
                    }
                }
            }
        }

        [Test]
        public void AnyIntersection()
        {
            var availQuery = AvailabilityController.AvailabilityIntersectionOrAdjacentExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            foreach (var item in _intersection)
            {
                if (!availQuery(_availabilityTransformation(item)))
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void NoIntersection()
        {
            var encapsulated = AvailabilityController.AvailabilityTimeWindowEncapsulatedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var left = AvailabilityController.AvailabilityLeftNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var right = AvailabilityController.AvailabilityRightNeighborExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var captured = AvailabilityController.AvailabilityTimeWindowsCapturedExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();
            var intersection = AvailabilityController.AvailabilityIntersectionOrAdjacentExpression(0, _mainWindow.StartTime, _mainWindow.EndTime).Compile();

            var intersectionFunctions = new List<Func<Availability, bool>>();

            intersectionFunctions.Add(encapsulated);
            intersectionFunctions.Add(left);
            intersectionFunctions.Add(right);
            intersectionFunctions.Add(captured);
            intersectionFunctions.Add(intersection);
        }
    }
}
