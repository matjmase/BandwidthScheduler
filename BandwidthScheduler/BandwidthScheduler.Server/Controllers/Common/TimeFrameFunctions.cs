using Azure.Core;
using BandwidthScheduler.Server.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BandwidthScheduler.Server.Controllers.Common
{
    public static class TimeFrameFunctions
    {
        #region Stitching

        public static bool StitchSidesAvailabilities(int userId, DateTime windowStart, DateTime windowEnd, Availability? encapsulate, Availability? left, Availability? right, out HashSet<Availability> toAdd, out HashSet<Availability> toRemove)
        {
            return StitchSidesCollection(windowStart, windowEnd, encapsulate, left, right, e => e.StartTime, e => e.EndTime, (s, e) => new Availability() { UserId = userId, StartTime = s, EndTime = e }, out toAdd, out toRemove);
        }
        public static bool StitchSidesCommitment(int userId, int teamId, DateTime windowStart, DateTime windowEnd, Commitment? encapsulate, Commitment? left, Commitment? right, out HashSet<Commitment> toAdd, out HashSet<Commitment> toRemove)
        {
            return StitchSidesCollection(windowStart, windowEnd, encapsulate, left, right, e => e.StartTime, e => e.EndTime, (s, e) => new Commitment() { UserId = userId, TeamId = teamId, StartTime = s, EndTime = e }, out toAdd, out toRemove);
        }

        public static bool StitchSidesCollection<T>(DateTime windowStart, DateTime windowEnd, T? encapsulate, T? left, T? right, Func<T, DateTime> start, Func<T, DateTime> end, Func<DateTime, DateTime, T> createNew, out HashSet<T> toAdd, out HashSet<T> toRemove) where T : class
        { 
            var toAddLocal = new HashSet<T>();
            var toRemoveLocal = new HashSet<T>();

            Action<DateTime, DateTime> addNew = (s, e) =>
            {
                toAddLocal.Add(createNew(s, e));
            };

            Action<T> remove = e => { toRemoveLocal.Add(e); };

            var retVal = StitchSides(windowStart, windowEnd, encapsulate, left, right, start, end, addNew, remove);

            toAdd = toAddLocal;
            toRemove = toRemoveLocal;

            return retVal;
        }

        public static bool StitchSides<T>(DateTime windowStart, DateTime windowEnd, T? encapsulate, T? left, T? right, Func<T, DateTime> start, Func<T, DateTime> end, Action<DateTime, DateTime> createNew, Action<T> remove) where T : class
        {
            if (encapsulate != null && (left != null || right != null))
            {
                return false;
            }

            if (encapsulate != null)
            {
                createNew(start(encapsulate), windowStart);
                createNew(windowEnd, end(encapsulate));
                remove(encapsulate);
            }
            else
            {
                if (left != null)
                {
                    createNew(start(left), windowStart);
                    remove(left);
                }
                if (right != null)
                {
                    createNew(windowEnd, end(right));
                    remove(right);
                }
            }

            return true;
        }

        #endregion

        #region Streaking

        public static bool CreateStreaksAvailability(IEnumerable<Availability> segmented, out List<Availability>? streaks)
        {
            return CreateStreaks(segmented, e => e.StartTime, e => e.EndTime, (avail, newEnd) => { avail.EndTime = newEnd; }, out streaks);
        }

        public static bool CreateStreaksCommitment(IEnumerable<Commitment> segmented, out List<Commitment>? streaks)
        {
            return CreateStreaks(segmented, e => e.StartTime, e => e.EndTime, (avail, newEnd) => { avail.EndTime = newEnd; }, out streaks);
        }

        public static bool CreateStreaks<T>(IEnumerable<T> segmented, Func<T, DateTime> getStart, Func<T, DateTime> getEnd, Action<T, DateTime> setEnd, out List<T>? streaks) where T : class
        {
            streaks = null;

            var sorted = segmented.OrderBy(e => getStart(e));

            var output = new List<T>();

            Func<T, T, bool> intersection = (t1, t2) =>
            {
                return !(getEnd(t1) <= getStart(t2) || getStart(t1) >= getEnd(t2));
            };

            T? lastApplicable = null;
            foreach (var applicability in sorted)
            {
                if (lastApplicable == null)
                {
                    lastApplicable = applicability;
                }
                else
                {   if (intersection(lastApplicable, applicability))
                    {
                        return false;
                    }
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

            streaks = output;

            return true;
        }

        #endregion

        #region Redundancy

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

        #endregion

        #region Seperation

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

        #endregion

        #region Validate No Intersection

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

        #endregion

        #region Validate Chain

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

        #endregion
    }
}
