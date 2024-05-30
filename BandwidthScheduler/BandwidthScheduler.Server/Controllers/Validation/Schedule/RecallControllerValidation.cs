using BandwidthScheduler.Server.Models.PublishController.Request;

namespace BandwidthScheduler.Server.Controllers.Validation.Schedule
{
    public static class RecallControllerValidation
    {
        public static bool ValidateRecallRequest(ScheduleRecallRequest request)
        {
            return request.Start < request.End;
        }
    }
}
