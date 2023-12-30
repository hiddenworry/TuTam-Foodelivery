using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IBranchService
    {
        Task<CommonResponse> CreateBranch(BranchCreatingRequest branchRequest);
        Task<CommonResponse> GetBranchDetailsForBranchAdmin(Guid userId);
        Task<CommonResponse> GetBranchDetailsForSystemAdmin(Guid branchId);
        Task<CommonResponse> GetBranchDetailsForUser(Guid branchId);
        Task<CommonResponse> GetBranchesAsync(
            string? name,
            BranchStatus? status,
            string? address,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        );
        Task<CommonResponse> UpdateBranch(BranchUpdatingRequest branchRequest, Guid branchId);
    }
}
