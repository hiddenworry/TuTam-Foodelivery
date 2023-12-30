using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IActivityTaskService
    {
        Task<CommonResponse> CreateTask(TaskCreaingRequest request, Guid onwerId);
        Task<CommonResponse> DeleteTask(Guid taskId, Guid ownerId);
        Task<CommonResponse> GetTask(
            int? page,
            int? pageSize,
            Guid ownerId,
            Guid? activityId,
            Guid? phaseId,
            string? Name,
            DateTime? StartDate,
            DateTime? EndDate
        );
        Task<CommonResponse> GetTaskDetail(Guid? taskId, Guid onwerId);
        Task<CommonResponse> UpdateTask(
            Guid onwerId,
            List<TaskUpdatingRequest> taskUpdatingRequest
        );
    }
}
