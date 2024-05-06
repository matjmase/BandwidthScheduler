namespace BandwidthScheduler.Server.Models.Availability.Response
{
    public class ClientCommitment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
