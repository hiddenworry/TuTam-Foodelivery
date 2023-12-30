using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class CharityUnitRepository : ICharityUnitRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public CharityUnitRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<CharityUnit?> FindCharityUnitByUserIdAsync(Guid userId)
        {
            return await _context.CharityUnits
                .Include(cu => cu.Charity)
                .FirstOrDefaultAsync(
                    cu => cu.UserId == userId && cu.Status == CharityUnitStatus.ACTIVE
                );
        }

        public async Task<CharityUnit?> FindUnverifyCharityUnitByUserIdAsync(Guid userId)
        {
            return await _context.CharityUnits
                .Include(cu => cu.Charity)
                .FirstOrDefaultAsync(
                    cu => cu.UserId == userId && cu.Status == CharityUnitStatus.UNVERIFIED
                );
        }

        public async Task<List<CharityUnit>> FindCharityUnitsByCharityIdAsync(Guid charityId)
        {
            return await _context.CharityUnits
                .Include(u => u.User)
                .Where(cu => cu.CharityId == charityId && cu.Status != CharityUnitStatus.DELETED)
                .ToListAsync();
        }

        public async Task<List<CharityUnit>> FindActiveCharityUnitsByCharityIdAsync(Guid charityId)
        {
            return await _context.CharityUnits
                .Include(u => u.User)
                .Where(cu => cu.CharityId == charityId && cu.Status == CharityUnitStatus.ACTIVE)
                .ToListAsync();
        }

        public async Task<List<CharityUnit>> FindCharityByStatusAsync(CharityUnitStatus status)
        {
            return await _context.CharityUnits
                .Where(cu => cu.Status == status && cu.Status != CharityUnitStatus.DELETED)
                .ToListAsync();
        }

        public async Task<int> CreateCharityUnitAsync(CharityUnit charityUnit)
        {
            _context.CharityUnits.Add(charityUnit);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateCharityUnitAsync(CharityUnit charity)
        {
            _context.CharityUnits.Update(charity);
            return await _context.SaveChangesAsync();
        }

        public async Task<CharityUnit?> FindCharityUnitByIdAsync(Guid charityUnitId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .FirstOrDefaultAsync(
                    cu => cu.Id == charityUnitId && cu.Status != CharityUnitStatus.DELETED
                );
        }

        public async Task<int> DeleteCharityUnitAsync(CharityUnit charity)
        {
            _context.CharityUnits.Remove(charity);
            return await _context.SaveChangesAsync();
        }

        public async Task<CharityUnit?> GetCharityUnitsByCharityIdAsync(Guid charityId)
        {
            return await _context.CharityUnits
                .Include(cu => cu.User)
                .Where(cu => cu.CharityId == charityId && cu.Status != CharityUnitStatus.DELETED)
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindCharityUnitsByIdForUserAsync(Guid charityUnitId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(
                    cu =>
                        cu.Id == charityUnitId
                        && cu.Status != CharityUnitStatus.DELETED
                        && cu.Status != CharityUnitStatus.UNVERIFIED
                )
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> GetLatestUpdateCharityUnitByUserIdForAdminAsync(Guid userId)
        {
            var latestCharityUnit = await _context.CharityUnits
                .Include(cu => cu.User)
                .Where(cu => cu.Status == CharityUnitStatus.ACTIVE && cu.UserId == userId)
                .OrderBy(cu => cu.CreatedDate)
                .FirstOrDefaultAsync();

            return latestCharityUnit;
        }

        public async Task<List<CharityUnit>?> GetCharityUnit(
            string? searchKeyWord,
            CharityUnitStatus? status,
            Guid? charityId
        )
        {
            var query = _context.CharityUnits
                .Include(cu => cu.Charity)
                .Include(cu => cu.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchKeyWord))
            {
                query = query.Where(cu => cu.Name.ToLower().Contains(searchKeyWord.ToLower()));
            }
            if (status != null)
            {
                query = query.Where(cu => cu.Status == status);
            }
            if (charityId != null)
            {
                query = query.Where(cu => cu.CharityId == charityId);
            }
            query = query.Where(
                cu =>
                    cu.Status != CharityUnitStatus.UNVERIFIED
                    && cu.Status != CharityUnitStatus.UNVERIFIED_UPDATE
            );
            return await query.ToListAsync();
        }

        public async Task<CharityUnit?> FindUnVerifyCharityUnitsByIdAsync(Guid charityUnitId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(cu => cu.Id == charityUnitId && cu.Status == CharityUnitStatus.UNVERIFIED)
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindUnVerifyUpdateCharityUnitsByIdAsync(Guid charityUnitId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(
                    cu => cu.Id == charityUnitId && cu.Status == CharityUnitStatus.UNVERIFIED_UPDATE
                )
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindActiveCharityUnitsByUserIdAsync(Guid userId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(cu => cu.UserId == userId && cu.Status == CharityUnitStatus.ACTIVE)
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindCharityUnitsByUserAndStatusIdAsync(
            Guid userId,
            CharityUnitStatus status
        )
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(cu => cu.UserId == userId && cu.Status == status)
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindUnverifyUpdateCharityUnitByUserIdAsync(Guid userId)
        {
            return await _context.CharityUnits
                .Include(c => c.User)
                .Where(
                    cu => cu.UserId == userId && cu.Status == CharityUnitStatus.UNVERIFIED_UPDATE
                )
                .FirstOrDefaultAsync();
        }

        public async Task<CharityUnit?> FindCharityUnitByUserIdOnlyAsync(Guid userId)
        {
            return await _context.CharityUnits.FirstOrDefaultAsync(cu => cu.UserId == userId);
        }
    }
}
