using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public PermissionRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<Permission?> GetPermissionByIdAsync(Guid id)
        {
            return await _context.Permissions.Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Permission?> AddPermissionAsync(Permission permission)
        {
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<Permission?> GetPermissionByNameAsync(string name)
        {
            return await _context.Permissions.Where(p => p.Name == name).FirstOrDefaultAsync();
        }

        public async Task<List<Permission>?> GetListPermissionAsync()
        {
            return await _context.Permissions.ToListAsync();
        }
    }
}
