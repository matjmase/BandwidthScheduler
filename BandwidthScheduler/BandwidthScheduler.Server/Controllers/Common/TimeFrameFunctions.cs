using Azure.Core;
using BandwidthScheduler.Server.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BandwidthScheduler.Server.Controllers.Common
{
    public static class TimeFrameFunctions
    {
        // Stitching 

        public static bool StitchSidesAvailabilities(int userId, DateTime windowStart, DateTime windowEnd, Availability? encapsulate, Availability? left, Availability? right, DbSet<Availability> db, out Availability? leftResult, out Availability? rightResult)
        {
            return StitchSides(windowStart, windowEnd, encapsulate, left, right, e => e.StartTime, e => e.EndTime, (s, e) => new Availability() { UserId = userId, StartTime = s, EndTime = e }, e => db.Remove(e), out leftResult, out rightResult);
        }
        public static bool StitchSidesCommitment(int userId, int teamId, DateTime windowStart, DateTime windowEnd, Commitment? encapsulate, Commitment? left, Commitment? right, DbSet<Commitment> db, out Commitment? leftResult, out Commitment? rightResult)
        {
            return StitchSides(windowStart, windowEnd, encapsulate, left, right, e => e.StartTime, e => e.EndTime, (s, e) => new Commitment() { UserId = userId, TeamId = teamId, StartTime = s, EndTime = e }, e => db.Remove(e), out leftResult, out rightResult);
        }

        public static bool StitchSides<T>(DateTime windowStart, DateTime windowEnd, T? encapsulate, T? left, T? right, Func<T, DateTime> start, Func<T, DateTime> end, Func<DateTime, DateTime, T> createNew, Action<T> remove, out T? leftResult, out T? rightResult) where T : class
        {
            leftResult = null;
            rightResult = null;

            if (encapsulate != null && (left != null || right != null))
            {
                return false;
            }

            if (encapsulate != null)
            {
                leftResult = createNew(start(encapsulate), windowStart);
                rightResult = createNew(windowEnd, end(encapsulate));
                remove(encapsulate);
            }
            else
            {
                if (left != null)
                {
                    leftResult = createNew(start(left), windowStart);
                    remove(left);
                }
                if (right != null)
                {
                    rightResult = createNew(windowEnd, end(right));
                    remove(right);
                }
            }

            return true;
        }

        // Streaks

        public static List<Availability> CreateStreaksAvailability(Availability[] segmented)
        {
            return CreateStreaks(segmented, e => e.StartTime, e => e.EndTime, (avail, newEnd) => { avail.EndTime = newEnd; });
        }

        public static List<Commitment> CreateStreaksCommitment(Commitment[] segmented)
        {
            return CreateStreaks(segmented, e => e.StartTime, e => e.EndTime, (avail, newEnd) => { avail.EndTime = newEnd; });
        }

        public static List<T> CreateStreaks<T>(T[] segmented, Func<T, DateTime> getStart, Func<T, DateTime> getEnd, Action<T, DateTime> setEnd) where T : class
        {
            var sorted = segmented.OrderBy(e => getStart(e));

            var output = new List<T>();

            T? lastApplicable = null;
            foreach (var applicability in sorted)
            {
                if (lastApplicable == null)
                {
                    lastApplicable = applicability;
                }
                else
                {
                    if (getEnd(lastApplicable) == getStart(applicability))
                    {
                        setEnd(lastApplicable, getEnd(applicability));
                    }
                    else
                    {
                        output.Add(lastApplicable);
                        lastApplicable = applicability;
                    }
                }
            }

            if (lastApplicable != null)
            {
                output.Add(lastApplicable);
            }

            return output;
        }

        // redundancy

        public static void IdentifyRedundancyAvailability(IEnumerable<Availability> input, IEnumerable<Availability> current, out HashSet<Availability> toAdd, out HashSet<Availability> toRemove)
        {
            Func<Availability, Availability, bool> isEquivalent = (f, s) => f.StartTime == s.StartTime && f.EndTime == s.EndTime;
            IdentifyRedundancy(input, current, e => e.StartTime, isEquivalent, out toAdd, out toRemove);
        }
        public static void IdentifyRedundancyCommitment(IEnumerable<Commitment> input, IEnumerable<Commitment> current, out HashSet<Commitment> toAdd, out HashSet<Commitment> toRemove)
        {
            Func<Commitment, Commitment, bool> isEquivalent = (f, s) => f.StartTime == s.StartTime && f.EndTime == s.EndTime;
            IdentifyRedundancy(input, current, e => e.StartTime, isEquivalent, out toAdd, out toRemove);
        }

        public static void IdentifyRedundancy<T>(IEnumerable<T> input, IEnumerable<T> current, Func<T, DateTime> start, Func<T, T, bool> isEquivalent, out HashSet<T> toAdd, out HashSet<T> toRemove)
        {
            toAdd = input.ToHashSet();
            toRemove = current.ToHashSet();

            var inputEnum = input.GetEnumerator();
            var currentEnum = current.GetEnumerator();

            var inputHasNext = inputEnum.MoveNext();
            var currentHasNext = currentEnum.MoveNext();

            while (inputHasNext && currentHasNext)
            {
                var inputValue = inputEnum.Current;
                var currentValue = currentEnum.Current;

                if (isEquivalent(inputValue, currentValue))
                {
                    toAdd.Remove(inputValue);
                    toRemove.Remove(currentValue);
                }

                if (start(inputValue) < start(currentValue))
                {
                    inputHasNext = inputEnum.MoveNext();
                }
                else
                {
                    currentHasNext = currentEnum.MoveNext();
                }
            }
        }

        // Seperation

        public static bool IdentifySeperation<T, K>(IEnumerable<T> collection1, Func<T, DateTime> getStart1, Func<T, DateTime> getEnd1, IEnumerable<K> collection2, Func<K, DateTime> getStart2, Func<K, DateTime> getEnd2)
        {
            Func<T, K, bool> intersection = (a, c) =>
            {
                return (
                !(getEnd2(c) <= getStart1(a) || getStart2(c) >= getEnd1(a))
                );
            };

            var col1Enum = collection1.GetEnumerator();
            var col2Enum = collection2.GetEnumerator();

            var availableHasNext = col1Enum.MoveNext();
            var commitmentHasNext = col2Enum.MoveNext();

            while (availableHasNext && commitmentHasNext)
            {
                var value1 = col1Enum.Current;
                var value2 = col2Enum.Current;

                if (intersection(value1, value2))
                {
                    return false;
                }

                if (getStart1(value1) < getStart2(value2))
                {
                    availableHasNext = col1Enum.MoveNext();
                }
                else
                {
                    commitmentHasNext = col2Enum.MoveNext();
                }
            }

            return true;
        }

        // Validate Sequential Time Frames

        public static bool ValidateTimeFrameNoIntersection<T>(IEnumerable<T> timeFrames, Func<T, DateTime> getStart, Func<T, DateTime> getEnd, out DateTime windowStart, out DateTime windowEnd)
        {
            windowStart = new DateTime();
            windowEnd = new DateTime();

            var sorted = timeFrames.OrderBy(e => getStart(e));

            DateTime? firstStartTime = null;
            DateTime? lastEndTime = null;

            foreach (var timeFrame in sorted)
            {
                if (getStart(timeFrame) >= getEnd(timeFrame))
                {
                    return false;
                }

                if (firstStartTime == null)
                {
                    firstStartTime = getStart(timeFrame);
                }

                if (lastEndTime != null && lastEndTime.Value > getStart(timeFrame))
                {
                    return false;
                }

                lastEndTime = getEnd(timeFrame);
            }

            if (firstStartTime == null || lastEndTime == null)
                return false;

            windowStart = firstStartTime.Value;
            windowEnd = lastEndTime.Value;

            return true;
        }

        public static bool ValidateTimeFrameChain<T>(IEnumerable<T> timeFrames, Func<T, DateTime> getStart, Func<T, DateTime> getEnd, out DateTime windowStart, out DateTime windowEnd)
        {
            windowStart = new DateTime();
            windowEnd = new DateTime();

            var sorted = timeFrames.OrderBy(e => getStart(e));

            DateTime? firstStartTime = null;
            DateTime? lastEndTime = null;

            foreach (var timeFrame in sorted)
            {
                if (getStart(timeFrame) >= getEnd(timeFrame))
                {
                    return false;
                }

                if (firstStartTime == null)
                {
                    firstStartTime = getStart(timeFrame);
                }

                if (lastEndTime != null && lastEndTime.Value != getStart(timeFrame))
                {
                    return false;
                }

                lastEndTime = getEnd(timeFrame);
            }

            if (firstStartTime == null || lastEndTime == null)
                return false;

            windowStart = firstStartTime.Value;
            windowEnd = lastEndTime.Value;

            return true;
        }
    }
}
