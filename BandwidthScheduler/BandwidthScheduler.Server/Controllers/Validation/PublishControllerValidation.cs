using Azure.Core;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;

namespace BandwidthScheduler.Server.Controllers.Validation
{
    public static class PublishControllerValidation
    {
        // Proposal

        public static bool ValidateProposalRequest(ScheduleProposalRequest request, out DateTime windowStart, out DateTime windowEnd)
        { 
            windowStart = default(DateTime);
            windowEnd = default(DateTime);

            if (request == null || request.Proposal.Length == 0 || request.SelectedTeam == null)
            {
                return false;
            }

            if (!ValidateProposalContinuity(request.Proposal, out windowStart, out windowEnd)) 
            {
                return false;
            }

            return true;
        }

        public static bool ValidateProposalContinuity(ScheduleProposalAmount[] scheduleProposals, out DateTime windowStart, out DateTime windowEnd)
        {
            return TimeFrameFunctions.ValidateTimeFrameChain(scheduleProposals, e => e.StartTime, e => e.EndTime, out windowStart, out windowEnd);
        }

        // Submit

        public static bool ValidateProposalSubmitRequest(ScheduleSubmitRequest submit, out DateTime windowStart, out DateTime windowEnd)
        {
            // validate proposal 

            if (!ValidateProposalRequest(submit.ProposalRequest, out windowStart, out windowEnd))
            {
                return false;
            }

            // validate response

            if (submit.ProposalResponse == null || submit.ProposalResponse.ProposalUsers == null)
            {
                return false;
            }
            else if (submit.ProposalResponse.ProposalUsers.Length == 0)
            {
                return true;
            }

            // validate request encapsulates response

            var submitStart = submit.ProposalResponse.ProposalUsers.CompareOrDefault((f,s) => f.StartTime < s.StartTime).StartTime;
            var submitEnd = submit.ProposalResponse.ProposalUsers.CompareOrDefault((f,s) => f.EndTime > s.EndTime).EndTime;

            return submitStart >= windowStart && submitEnd <= windowEnd;
        }

        public static bool ValidateCommitmentSeperation(IEnumerable<Commitment> newCommitments, IEnumerable<Commitment> oldCommitments)
        {
            return TimeFrameFunctions.IdentifySeperation(newCommitments, c => c.StartTime, c => c.EndTime, oldCommitments, c => c.StartTime, c => c.EndTime);
        }
    }
}
