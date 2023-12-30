namespace DataAccess.Models.Requests
{
    public class TaskCreaingRequest
    {
        public List<TaskRequest> TaskRequests { get; set; }

        public Guid PhaseId { get; set; }
    }
}
