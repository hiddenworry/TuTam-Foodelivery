using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class CollaboratorRepository : ICollaboratorRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public CollaboratorRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int>? CreateCollaboratorAsync(CollaboratorApplication collaborator)
        {
            var rs = _context.CollaboratorApplications.Add(collaborator);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateCollaboratorAsync(CollaboratorApplication collaborator)
        {
            var rs = _context.CollaboratorApplications.Update(collaborator);
            return await _context.SaveChangesAsync();
        }

        public async Task<CollaboratorApplication?> FindCollaboratorByIdAsync(Guid collaboratorId)
        {
            return await _context.CollaboratorApplications
                .Include(u => u.User)
                .FirstOrDefaultAsync(c => c.Id == collaboratorId);
        }

        public async Task<CollaboratorApplication?> FindCollaboratorByUserIdAsync(Guid userId)
        {
            return await _context.CollaboratorApplications
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<int> DeleteCollaboratorAsync(CollaboratorApplication collaborator)
        {
            _context.CollaboratorApplications.Remove(collaborator);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<CollaboratorApplication>?> GetCollaboratorByStatusAsync(
            CollaboratorStatus? status
        )
        {
            var query = _context.CollaboratorApplications.Include(c => c.User).AsQueryable();
            if (status != null)
            {
                query = query.Where(c => c.Status == status);
            }

            return await query.Where(c => c.Status != CollaboratorStatus.DELETED).ToListAsync();
        }

        public async Task<int> CountCollaboratorByStatus(
            CollaboratorStatus? status,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var query = _context.CollaboratorApplications.AsQueryable();

            if (status != null)
            {
                // Filter by status
                query = query.Where(a => a.Status == status);
            }

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.CreatedDate >= startDate);
            }

            if (endDate != null && startDate == null)
            {
                query = query.Where(a => a.CreatedDate <= endDate);
            }
            if (endDate != null && startDate != null)
            {
                query = query.Where(a => a.CreatedDate <= endDate && a.CreatedDate >= startDate);
            }

            int count = await query.CountAsync();

            return count;
        }

        public async Task<List<CollaboratorApplication>?> FindCollaboratorActiveAsync()
        {
            return await _context.CollaboratorApplications
                .Include(u => u.User)
                .Where(
                    a =>
                        a.Status == CollaboratorStatus.ACTIVE
                        && a.User.IsCollaborator == true
                        && a.User.Status == UserStatus.ACTIVE
                )
                .ToListAsync();
        }
    }
}
