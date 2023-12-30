using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IActivityRoleService
    {
        Task<CommonResponse> CreateActivityRoleByActivtyId(
            ActivityRoleCreatingRequest request,
            Guid userId
        );
        Task<CommonResponse> GetListOfActivityRole(Guid activityId);
        Task<CommonResponse> UpdateActivityRoleById(
            List<ActivityRoleUpdatingRequest> request,
            Guid userId,
            Guid activityId
        );
    }
}
