using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class StockUpdatedHistoryDetailRepository : IStockUpdatedHistoryDetailRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public StockUpdatedHistoryDetailRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddStockUpdatedHistoryDetailAsync(
            StockUpdatedHistoryDetail stockUpdatedHistoryDetail
        )
        {
            await _context.StockUpdatedHistoryDetails.AddAsync(stockUpdatedHistoryDetail);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> AddStockUpdatedHistoryDetailsAsync(
            List<StockUpdatedHistoryDetail> stockUpdatedHistoryDetails
        )
        {
            int rs = 0;
            foreach (StockUpdatedHistoryDetail item in stockUpdatedHistoryDetails)
            {
                rs += await AddStockUpdatedHistoryDetailAsync(item);
            }
            return rs;
        }

        public async Task<int> DeleteStockUpdatedHistoryDetailsAsync(
            List<StockUpdatedHistoryDetail> oldStockUpdatedHistoryDetails
        )
        {
            int rs = 0;
            foreach (StockUpdatedHistoryDetail item in oldStockUpdatedHistoryDetails)
            {
                rs += await DeleteStockUpdatedHistoryDetailAsync(item);
            }
            return rs;
        }

        public async Task<int> DeleteStockUpdatedHistoryDetailAsync(
            StockUpdatedHistoryDetail stockUpdatedHistoryDetails
        )
        {
            _context.StockUpdatedHistoryDetails.Remove(stockUpdatedHistoryDetails);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsByBranchId(
            Guid branchId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            //var query = _context.StockUpdatedHistoryDetails
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.DonatedRequest)
            //    .ThenInclude(a => a!.User)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.AidRequest)
            //    .ThenInclude(a => a!.CharityUnit)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.AidItem)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DonatedItem)
            //    .Include(a => a.StockUpdatedHistory)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.Branch)
            //    .Include(a => a.Stock)
            //    .ThenInclude(s => s!.Activity)
            //    .Include(a => a.Stock)
            //    .ThenInclude(s => s!.User)
            //    .Include(a => a.StockUpdatedHistory)
            //    .ThenInclude(a => a!.Branch)
            //    .Where(a => a.Stock != null)
            //    .Where(a => a.StockUpdatedHistory.IsPrivate == false)
            //    .AsQueryable();

            var query = _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.User)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Activity)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suhd => suhd.StockUpdatedHistory)
                .ThenInclude(suh => suh.Branch)
                .Where(a => a.StockId != null)
                .Where(a => a.StockUpdatedHistory.IsPrivate == false)
                .Where(a => a.Quantity > 0)
                .AsQueryable();

            query = query.Where(a => a.StockUpdatedHistory.BranchId == branchId);
            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate > startDate);
            }
            else if (startDate == null && endDate != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate < endDate);
            }
            else if (startDate != null && endDate != null)
            {
                query = query.Where(
                    a =>
                        a.StockUpdatedHistory.CreatedDate >= startDate
                        && a.StockUpdatedHistory.CreatedDate <= endDate
                );
            }

            return await query
                .OrderByDescending(a => a.StockUpdatedHistory.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsByActivityId(
            Guid activityId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            //var query = _context.StockUpdatedHistoryDetails
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.DonatedRequest)
            //    .ThenInclude(a => a!.User)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.AidRequest)
            //    .ThenInclude(a => a!.CharityUnit)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.AidItem)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DonatedItem)
            //    .Include(a => a.StockUpdatedHistory)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.Branch)
            //    .Include(a => a.Stock)
            //    .ThenInclude(s => s!.Activity)
            //    .Include(a => a.Stock)
            //    .ThenInclude(s => s!.User)
            //    .Include(a => a.StockUpdatedHistory)
            //    .ThenInclude(a => a!.Branch)
            //    .Where(a => a.Stock != null)
            //    .Where(a => a.StockUpdatedHistory.IsPrivate == false)
            //    .AsQueryable();

            var query = _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.User)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Activity)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suhd => suhd.StockUpdatedHistory)
                .ThenInclude(suh => suh.Branch)
                .Where(a => a.StockId != null)
                .Where(a => a.StockUpdatedHistory.IsPrivate == false)
                .Where(a => a.Quantity > 0)
                .AsQueryable();

            query = query.Where(a => a.StockId != null && a.Stock!.ActivityId == activityId);
            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate >= startDate);
            }
            else if (startDate == null && endDate != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate <= endDate);
            }
            else if (startDate != null && endDate != null)
            {
                query = query.Where(
                    a =>
                        a.StockUpdatedHistory.CreatedDate >= startDate
                        && a.StockUpdatedHistory.CreatedDate <= endDate
                );
            }

            return await query
                .OrderByDescending(a => a.StockUpdatedHistory.CreatedDate)
                .ToListAsync();
        }

        public async Task<
            List<StockUpdatedHistoryDetail>
        > GetStockUpdatedHistoryDetailsByDeliveryItemIdAsync(Guid deliveryItemId)
        {
            return await _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.StockUpdatedHistory)
                .Include(suh => suh.Stock)
                .ThenInclude(s => s!.Activity)
                .Where(suhd => suhd.DeliveryItemId == deliveryItemId)
                .ToListAsync();
        }

        public async Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryByCharityUnitId(
            Guid charityUnitId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            //var query = _context.StockUpdatedHistoryDetails
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.DonatedRequest)
            //    .ThenInclude(a => a!.User)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.AidRequest)
            //    .ThenInclude(a => a!.CharityUnit)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.AidItem)
            //    .Include(a => a.StockUpdatedHistory)
            //    .Include(a => a.DeliveryItem)
            //    .ThenInclude(a => a!.DeliveryRequest)
            //    .ThenInclude(a => a.Branch)
            //    .Include(a => a.Stock)
            //    .ThenInclude(s => s!.Activity)
            //    .Include(a => a.StockUpdatedHistory)
            //    .ThenInclude(a => a!.Branch)
            //    .Where(a => a.StockUpdatedHistory.IsPrivate == false)
            //    .Where(
            //        a =>
            //            a.DeliveryItem != null
            //            && a.DeliveryItem.DeliveryRequest != null
            //            && a.DeliveryItem.DeliveryRequest.AidRequest != null
            //    )
            //    .Where(a => a.Stock != null)
            //    .Include(a => a.Stock)
            //    .AsQueryable();

            var query = _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.User)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Activity)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suhd => suhd.StockUpdatedHistory)
                .ThenInclude(suh => suh.Branch)
                .Where(a => a.StockId != null)
                .Where(a => a.StockUpdatedHistory.IsPrivate == false)
                .Where(a => a.Quantity > 0)
                .AsQueryable();

            query = query.Where(
                a => a.AidRequestId != null && a.AidRequest!.CharityUnitId == charityUnitId
            );

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate >= startDate);
            }
            else if (startDate == null && endDate != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate <= endDate);
            }
            else if (startDate != null && endDate != null)
            {
                query = query.Where(
                    a =>
                        a.StockUpdatedHistory.CreatedDate >= startDate
                        && a.StockUpdatedHistory.CreatedDate <= endDate
                );
            }

            return await query
                .OrderByDescending(a => a.StockUpdatedHistory.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryDetailsForAdmin(
            Guid? charityUnitId,
            Guid? branchId,
            StockUpdatedHistoryType? type,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var query = _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.User)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Activity)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suhd => suhd.StockUpdatedHistory)
                .ThenInclude(suh => suh.Branch)
                .Where(a => a.StockId != null)
                .Where(a => a.Quantity > 0)
                .AsQueryable();

            if (charityUnitId != null)
            {
                query = query.Where(
                    a => a.AidRequestId != null && a.AidRequest!.CharityUnitId == charityUnitId
                );
            }
            if (type != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.Type == type);
            }
            if (branchId != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.BranchId == branchId);
            }
            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate >= startDate);
            }
            else if (startDate == null && endDate != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate <= endDate);
            }
            else if (startDate != null && endDate != null)
            {
                query = query.Where(
                    a =>
                        a.StockUpdatedHistory.CreatedDate >= startDate
                        && a.StockUpdatedHistory.CreatedDate <= endDate
                );
            }

            return await query
                .OrderByDescending(a => a.StockUpdatedHistory.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<StockUpdatedHistoryDetail>> GetStockUpdateHistoryOfContributor(
            Guid userId,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var query = _context.StockUpdatedHistoryDetails
                .Include(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.User)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Activity)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .Include(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suhd => suhd.StockUpdatedHistory)
                .ThenInclude(suh => suh.Branch)
                .Where(a => a.StockId != null)
                .Where(
                    a =>
                        a.StockUpdatedHistory.IsPrivate == false
                        && a.StockUpdatedHistory.Type == StockUpdatedHistoryType.IMPORT
                )
                .Where(a => a.Quantity > 0)
                .AsQueryable();

            query = query.Where(a => a.StockId != null && a.Stock!.UserId == userId);
            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate >= startDate);
            }
            else if (startDate == null && endDate != null)
            {
                query = query.Where(a => a.StockUpdatedHistory.CreatedDate <= endDate);
            }
            else if (startDate != null && endDate != null)
            {
                query = query.Where(
                    a =>
                        a.StockUpdatedHistory.CreatedDate >= startDate
                        && a.StockUpdatedHistory.CreatedDate <= endDate
                );
            }

            return await query
                .OrderByDescending(a => a.StockUpdatedHistory.CreatedDate)
                .ToListAsync();
        }
    }
}
