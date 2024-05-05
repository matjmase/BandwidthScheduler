using BandwidthScheduler.Server.Models.PublishController.Response;

namespace BandwidthScheduler.Server.Models.PublishController.Request
{
    public class ScheduleSubmitRequest
    {
        public ScheduleProposalRequest ProposalRequest { get; set; }    
        public ScheduleProposalResponse ProposalResponse { get; set; }  
    }
}
