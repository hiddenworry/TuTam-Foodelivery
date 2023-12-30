using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class AidItemRepository : IAidItemRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public AidItemRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAidItemsAsync(List<AidItem> aidItems)
        {
            int rs = 0;
            foreach (AidItem item in aidItems)
            {
                rs += await CreateAidItemAsync(item);
            }
            return rs;
        }

        public async Task<int> CreateAidItemAsync(AidItem aidItem)
        {
            await _context.AidItems.AddAsync(aidItem);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> UpdateAidItemsAsync(List<AidItem> aidItems)
        {
            int rs = 0;
            foreach (AidItem item in aidItems)
            {
                rs += await UpdateAidItemAsync(item);
            }
            return rs;
        }

        public async Task<int> UpdateAidItemAsync(AidItem aidItem)
        {
            _context.AidItems.Update(aidItem);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<AidItem?> GetAidItemForActivityByIdAsync(Guid aidItemId)
        {
            return await _context.AidItems
                .Include(ai => ai.AidRequest)
                .ThenInclude(ar => ar.AcceptableAidRequests)
                .FirstOrDefaultAsync(
                    ai => ai.Id == aidItemId && ai.Status == AidItemStatus.ACCEPTED
                );
        }

        public async Task<AidItem?> FindAidItemByIdAsync(Guid id)
        {
            return await _context.AidItems
                .Include(ai => ai.Item)
                .FirstOrDefaultAsync(di => di.Id == id);
        }
    }
}
