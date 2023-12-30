using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IActivityMemberService
    {
        Task<CommonResponse> CheckMemberOfActivity(Guid userId, Guid activityId);
        Task<CommonResponse> ConfirmMemberApplication(
            Guid memberId,
            Guid userId,
            ConfirmActivityApplicationRequest request
        );
        Task<CommonResponse> CreateActivityMemberApplication(
            Guid userId,
            ActivityApplicationRequest request
        );

        Task<CommonResponse> GetActivityMemberApplication(
            Guid? activityId,
            Guid onwerId,
            ActivityMemberStatus? status,
            int? page,
            int? pageSize,
            SortType? sortType,
            string? role
        );
    }
}
