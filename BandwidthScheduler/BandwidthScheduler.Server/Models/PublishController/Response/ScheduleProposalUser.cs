namespace BandwidthScheduler.Server.Models.PublishController.Response
{
    public class ScheduleProposalUser
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
