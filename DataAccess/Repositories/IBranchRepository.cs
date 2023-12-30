using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IBranchRepository
    {
        Task<Branch?> CreateBranchAsync(Branch branch);
        Task<Branch?> FindActiveBranchByIdAsync(Guid branchId);
        Task<Branch?> FindBranchByBranchAdminIdAsync(Guid userId);
        Task<Branch?> FindBranchByIdAsync(Guid branchId);
        Task<Branch?> FindBranchDetailsByIdAsync(Guid branchId);
        Task<List<Branch>> GetBranchesAsync(string? name, BranchStatus? status, string? address);
        Task<Branch?> UpdateBranchAsync(Branch branch);
    }
}
