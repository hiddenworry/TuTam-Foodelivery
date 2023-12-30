using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IActivityMemberRepository
    {
        Task<int> CreateActivityMemberAsync(ActivityMember activityMember);
        Task<ActivityMember> CreateActivityMemberReturnObjectAsync(ActivityMember activityMember);
        Task<int> DeleteActivityMemberAsync(ActivityMember activityMember);
        Task<ActivityMember?> FindActiveActivityMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        );
        Task<ActivityMember?> FindActivityMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        );
        Task<ActivityMember?> FindActivityMemberByIdAsync(Guid activityMemberId);
        Task<List<ActivityMember>?> FindMemberApplicationAsync(
            Guid? ownerId,
            Guid? activityId,
            ActivityMemberStatus? status
        );
        Task<List<ActivityMember>?> FindMemberByActivityIdAndUserIdAsync(
            Guid activityId,
            Guid userId
        );
        Task<List<ActivityMember>?> FindMemberByActivityIdAsync(
            Guid activityId,
            ActivityMemberStatus status
        );
        Task<List<ActivityMember>?> FindMemberByUserIdAndActivityIdAsync(
            Guid userId,
            Guid activityId,
            ActivityMemberStatus status
        );

        Task<int> UpdateActivityMemberAsync(ActivityMember activityMember);
    }
}
