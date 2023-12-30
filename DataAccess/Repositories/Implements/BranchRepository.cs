using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class BranchRepository : IBranchRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public BranchRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<Branch?> FindActiveBranchByIdAsync(Guid branchId)
        {
            return await _context.Branches.FirstOrDefaultAsync(
                b => b.Id == branchId && b.Status == BranchStatus.ACTIVE
            );
        }

        public async Task<Branch?> FindBranchByIdAsync(Guid branchId)
        {
            return await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        }

        public async Task<List<Branch>> GetBranchesAsync(
            string? name,
            BranchStatus? status,
            string? address
        )
        {
            return await _context.Branches
                .Where(
                    branch =>
                        (name != null ? branch.Name.ToUpper().Contains(name.ToUpper()) : true)
                        && (status != null ? branch.Status == status : true)
                        && (
                            address != null
                                ? branch.Address.ToUpper().Contains(address.ToUpper())
                                : true
                        )
                )
                .ToListAsync();
        }

        public async Task<Branch?> CreateBranchAsync(Branch branch)
        {
            try
            {
                var rs = _context.Branches.Add(branch);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Branch?> UpdateBranchAsync(Branch branch)
        {
            try
            {
                var rs = _context.Branches.Update(branch);
                await _context.SaveChangesAsync();
                return rs.Entity;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Branch?> FindBranchDetailsByIdAsync(Guid branchId)
        {
            return await _context.Branches
                .Include(b => b.BranchAdmin)
                .FirstOrDefaultAsync(b => b.Id == branchId);
        }

        public async Task<Branch?> FindBranchByBranchAdminIdAsync(Guid userId)
        {
            return await _context.Branches
                .Include(a => a.BranchAdmin)
                .FirstOrDefaultAsync(b => b.BranchAdminId == userId);
        }
    }
}
