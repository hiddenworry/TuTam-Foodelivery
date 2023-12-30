using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class DeliveryItemRepository : IDeliveryItemRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public DeliveryItemRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddDeliveryItemsAsync(List<DeliveryItem> deliveryItems)
        {
            int rs = 0;
            foreach (DeliveryItem deliveryItem in deliveryItems)
            {
                rs += await CreateDeliveryItemAsync(deliveryItem);
            }
            return rs;
        }

        public async Task<int> CreateDeliveryItemAsync(DeliveryItem deliveryItem)
        {
            await _context.DeliveryItems.AddAsync(deliveryItem);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<List<DeliveryItem>> GetDeliveryItemsByDeliveryRequestIdAsync(
            Guid deliveryRequestId
        )
        {
            List<DeliveryItem> deliveryItems = await _context.DeliveryItems
                .Include(di => di.DonatedItem)
                .ThenInclude(di => di!.Item)
                .Include(di => di.AidItem)
                .ThenInclude(ai => ai!.Item)
                .Where(di => di.DeliveryRequestId == deliveryRequestId)
                .ToListAsync();

            return deliveryItems;
        }

        public async Task<
            List<DeliveryItem>
        > GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(Guid itemId, Guid branchId)
        {
            return await _context.DeliveryItems
                .Include(di => di.DeliveryRequest)
                .Include(di => di.AidItem)
                .Include(di => di.StockUpdatedHistoryDetails)
                .Where(
                    di =>
                        di.AidItem != null
                        && itemId == di.AidItem.ItemId
                        && di.DeliveryRequest.BranchId == branchId
                        && di.DeliveryRequest.AidRequestId != null
                        && (
                            di.StockUpdatedHistoryDetails
                                .Where(suhd => suhd.StockId == null)
                                .Count() > 0
                        )
                        && di.DeliveryRequest.Status != DeliveryRequestStatus.EXPIRED
                        && di.DeliveryRequest.Status != DeliveryRequestStatus.CANCELED
                )
                .ToListAsync();
        }

        public async Task<int> UpdateDeliveryItemAsync(DeliveryItem deliveryItem)
        {
            _context.DeliveryItems.Update(deliveryItem);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> UpdateDeliveryItemsAsync(List<DeliveryItem> deliveryItems)
        {
            int rs = 0;
            foreach (DeliveryItem item in deliveryItems)
            {
                rs += await UpdateDeliveryItemAsync(item);
            }
            return rs;
        }

        public async Task<List<DeliveryItem>?> GetByDeliveredItemByCharityUnitId(Guid charityId)
        {
            var query = _context.DeliveryItems
                .Include(a => a.DeliveryRequest)
                .ThenInclude(a => a.AidRequest)
                .Where(
                    a =>
                        a.DeliveryRequest.AidRequest != null
                        && a.AidItem != null
                        && a.DeliveryRequest.AidRequest.CharityUnitId == charityId
                        && a.DeliveryRequest.Status == DeliveryRequestStatus.FINISHED
                )
                .OrderBy(a => a.DeliveryRequest.CreatedDate)
                .AsQueryable();
            return await query.ToListAsync();
        }
    }
}
