using Azure.Core;
using BandwidthScheduler.Server.Common.Extensions;
using BandwidthScheduler.Server.Controllers.Common;
using BandwidthScheduler.Server.DbModels;
using BandwidthScheduler.Server.Models.PublishController.Request;
using BandwidthScheduler.Server.Models.PublishController.Response;

namespace BandwidthScheduler.Server.Controllers.Validation
{
    public static class PublishControllerValidation
    {
        // Proposal

        public static bool ValidateProposalRequest(ScheduleProposalRequest request, out DateTime windowStart, out DateTime windowEnd)
        { 
            windowStart = default(DateTime);
            windowEnd = default(DateTime);

            if (request.Proposal.Any(e => e.Employees < 0))
            {
                return false;
            }    

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

        public static bool ProposalSubmitReproducibilityCheck(ScheduleSubmitRequest submitRequest)
        {
            var availabilities = submitRequest.ProposalResponse.ProposalUsers.Select(e => new Availability() { UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime, User = new User() { Email = e.Email } }).ToArray();
            var availabilityDictionary = availabilities.ToDictionaryAggregate(e => e.UserId);

            // perform scoping
            var userProposals = ScheduleGeneration.ScopeStreakToWindow(availabilityDictionary, submitRequest.ProposalRequest.Proposal);

            // order them the same way
            var respSorted = submitRequest.ProposalResponse.ProposalUsers.OrderBy(e => e.UserId).ThenBy(e => e.StartTime).ThenBy(e => e.EndTime).ToArray();
            var computedSorted = userProposals.OrderBy(e => e.UserId).ThenBy(e => e.StartTime).ThenBy(e => e.EndTime).ToArray();

            // make sure the two collections are synchronous
            if (computedSorted.Length != respSorted.Length)
            {
                return false;
            }

            // each is equivalent
            Func<ScheduleProposalUser, ScheduleProposalUser, bool> availabilitiesEquivalent = (avail1, avail2) =>
            {
                return avail1.UserId == avail2.UserId && avail1.StartTime == avail2.StartTime && avail1.EndTime == avail2.EndTime;
            };

            for (var i = 0; i < respSorted.Length; i++)
            {
                if (!availabilitiesEquivalent(respSorted[i], computedSorted[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ProposalSubmitDatabaseCheck(Availability[] dbEntities, ScheduleSubmitRequest submitRequest, out List<Availability>? remove, out List<Availability>? add, out Commitment[]? commitments)
        {
            remove = null;
            add = null;
            commitments = null;

            var availDict = dbEntities.ToDictionaryAggregate(e => e.UserId);

            foreach (var kv in availDict)
            {
                availDict[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }

            var proposalDict = submitRequest.ProposalResponse.ProposalUsers.ToDictionaryAggregate(e => e.UserId);

            foreach (var kv in proposalDict)
            {
                proposalDict[kv.Key] = kv.Value.OrderBy(e => e.StartTime).ToArray();
            }

            var removedAvailabilities = new List<Availability>();
            var addedAvailabilities = new List<Availability>();

            Func<Availability, DateTime> availStart = e => e.StartTime;
            Func<Availability, DateTime> availEnd = e => e.EndTime;

            Func<ScheduleProposalUser, DateTime> propStart = e => e.StartTime;
            Func<ScheduleProposalUser, DateTime> propEnd = e => e.EndTime;

            Action<int, DateTime, DateTime> addAvailFunc = (userId, startTime, endTime) =>
            {
                addedAvailabilities.Add(new Availability() { UserId = userId, StartTime = startTime, EndTime = endTime });
            };

            Action<Availability> removeAvailFunc = e =>
            {
                removedAvailabilities.Add(e);
            };

            if (!ProcessAvailabilitiesAndProposals(availDict, availStart, availEnd, proposalDict, propStart, propEnd, addAvailFunc, removeAvailFunc, e => { }))
            {
                return false;
            }

            remove = removedAvailabilities;
            add = addedAvailabilities;
            commitments = proposalDict.SelectMany(e => e.Value).Select(e => new Commitment() { TeamId = submitRequest.ProposalRequest.SelectedTeam.Id, UserId = e.UserId, StartTime = e.StartTime, EndTime = e.EndTime }).ToArray();

            return true;
        }

        public static bool ProcessAvailabilitiesAndProposals<T, K>(Dictionary<int, T[]> availabilities, Func<T, DateTime> availStartFunc, Func<T, DateTime> availEndFunc, Dictionary<int, K[]> proposals, Func<K, DateTime> proposalStartFunc, Func<K, DateTime> proposalEndFunc, Action<int, DateTime, DateTime> addAvailability, Action<T> removeAvailability, Action<K> addCommitment)
        {
            foreach (var kv in proposals)
            {
                var userId = kv.Key;

                var availEnum = availabilities[userId].AsEnumerable().GetEnumerator();
                var proposalEnum = proposals[userId].AsEnumerable().GetEnumerator();

                bool availHasNext = false;
                bool proposalHasNext = false;

                DateTime availStart = new DateTime();
                DateTime availEnd = new DateTime();

                DateTime propStart = new DateTime();
                DateTime propEnd = new DateTime();

                Action IncrementAvail = () =>
                {
                    availHasNext = availEnum.MoveNext();
                    if (availHasNext)
                    {
                        availStart = availStartFunc(availEnum.Current);
                        availEnd = availEndFunc(availEnum.Current);
                    }
                };

                Action IncrementProposal = () =>
                {
                    proposalHasNext = proposalEnum.MoveNext();
                    if (proposalHasNext)
                    {
                        propStart = proposalStartFunc(proposalEnum.Current);
                        propEnd = proposalEndFunc(proposalEnum.Current);
                    }
                };

                IncrementAvail();
                IncrementProposal();

                while (availHasNext && proposalHasNext)
                {
                    var availCurr = availEnum.Current;
                    var proposalCurr = proposalEnum.Current;

                    // Exception
                    if (availStart > propStart)
                    {
                        return false;
                    }

                    // End from available is before propstart
                    if (availEnd <= propStart)
                    {
                        // check for tail 
                        if (availHasNext && availStart != availStartFunc(availEnum.Current) && availStart != availEnd)
                        {
                            addAvailability(userId, availStart, availEnd);
                        }

                        // no collision
                        IncrementAvail();
                        continue;
                    }
                    else // collision
                    {
                        removeAvailability(availEnum.Current);

                        // check for head
                        if (availStart < propStart)
                        {
                            addAvailability(userId, availStart, propStart);
                        }

                        if (availEnd >= propEnd) // avail encapsulated
                        {
                            availStart = propEnd;

                            addCommitment(proposalEnum.Current);

                            IncrementProposal();
                            continue;
                        }
                        else // avail got first part
                        {
                            propStart = availEnd;

                            IncrementAvail();
                            continue;
                        }
                    }
                }

                if (proposalHasNext)
                {
                    return false;
                }

                // check for tail 
                if (availHasNext && availStart != availStartFunc(availEnum.Current) && availStart != availEnd)
                {
                    addAvailability(userId, availStart, availEnd);
                }
            }

            return true;
        }
    }
}
