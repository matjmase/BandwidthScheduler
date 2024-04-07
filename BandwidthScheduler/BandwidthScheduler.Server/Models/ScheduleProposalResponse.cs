namespace BandwidthScheduler.Server.Models
{
    public class ScheduleProposalResponse
    {
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Email { get; set; }   
    }
}
