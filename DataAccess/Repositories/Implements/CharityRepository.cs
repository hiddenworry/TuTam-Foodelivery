using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class CharityRepository : ICharityRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public CharityRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<List<Charity>> GetCharitiesAsync(
            CharityStatus? status,
            string? name,
            bool isWaitingToUpdate
        )
        {
            var query = _context.Charities
                .Include(c => c.CharityUnits)
                .Where(
                    c =>
                        (status == null || c.Status == status)
                        && (string.IsNullOrEmpty(name) || c.Name.ToUpper().Contains(name.ToUpper()))
                        && c.Status != CharityStatus.DELETED
                );

            if (isWaitingToUpdate)
            {
                query = query.Where(
                    c => c.CharityUnits.Any(b => b.Status == CharityUnitStatus.UNVERIFIED_UPDATE)
                );
            }

            return await query.ToListAsync();
        }

        public async Task<int> CreateCharityAsync(Charity charity)
        {
            _context.Charities.Add(charity);
            return await _context.SaveChangesAsync();
        }

        public async Task<Charity?> GetCharityById(Guid charityId)
        {
            return await _context.Charities
                .Include(c => c.CharityUnits)
                .Where(c => c.Id == charityId && c.Status != CharityStatus.DELETED)
                .FirstOrDefaultAsync();
        }

        public async Task<int> DeleteCharityAsync(Charity charity)
        {
            _context.Charities.Remove(charity);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateCharityAsync(Charity charity)
        {
            try
            {
                _context.Charities.Update(charity);
                return await _context.SaveChangesAsync();
            }
            catch
            {
                return 0;
            }
        }

        public async Task<Charity?> GetCharityByEmail(string email)
        {
            try
            {
                return await _context.Charities
                    .Include(c => c.CharityUnits)
                    .Where(c => c.Email == email && c.Status != CharityStatus.DELETED)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}
