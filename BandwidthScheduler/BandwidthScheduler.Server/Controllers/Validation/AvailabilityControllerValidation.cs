using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;

namespace BandwidthScheduler.Server.Controllers.Validation
{
    public static class AvailabilityControllerValidation
    {
        public static bool ValidateTimeFrames(DateTime start, DateTime end, Availability[] timeFrames)
        {
            if (!TimeFrameFunctions.ValidateTimeFrameNoIntersection(timeFrames, e => e.StartTime, e => e.EndTime, out var windowStart, out var windowEnd))
            {
                return false;
            }

            if (windowStart < start)
            {
                return false;
            }

            if (windowEnd > end)
            {
                return false;
            }

            return true;
        }

        public static bool ValidateAvaiabilityCommitmentSeperation(IEnumerable<Availability> availabilities, IEnumerable<Commitment> commitments)
        {
            return TimeFrameFunctions.IdentifySeperation(availabilities, a => a.StartTime, a => a.EndTime, commitments, c => c.StartTime, c => c.EndTime);
        }
    }
}
