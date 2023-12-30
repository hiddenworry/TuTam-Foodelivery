using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class StockUpdatedHistoryRepository : IStockUpdatedHistoryRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public StockUpdatedHistoryRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddStockUpdatedHistoryAsync(StockUpdatedHistory stockUpdatedHistory)
        {
            await _context.StockUpdatedHistories.AddAsync(stockUpdatedHistory);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        //public int CountDirectDonationAsync(DateTime? startDate, DateTime? endDate)
        //{
        //    return _context.StockUpdatedHistories
        //        .Where(
        //            suh =>
        //                suh.DirectDonorId != null
        //                && (startDate == null ? true : suh.CreatedDate >= startDate)
        //                && (endDate == null ? true : suh.CreatedDate <= endDate)
        //        )
        //        .Count();
        //}



        public async Task<
            List<StockUpdatedHistory>
        > FindStockUpdatedHistoriesBySelfShippingAidRequestIdAsync(Guid aidRequestId)
        {
            return await _context.StockUpdatedHistories
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.AidRequest)
                .Where(
                    suh =>
                        suh.StockUpdatedHistoryDetails.FirstOrDefault()!.AidRequestId
                            == aidRequestId
                        && suh.StockUpdatedHistoryDetails
                            .FirstOrDefault()!
                            .AidRequest!.IsSelfShipping
                )
                .ToListAsync();
        }

        public async Task<List<StockUpdatedHistory>> FindStockUpdatedHistoriesByAidRequestIdAsync(
            Guid aidRequestId
        )
        {
            List<StockUpdatedHistory> stockUpdatedHistories = await _context.StockUpdatedHistories
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.AidRequest)
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.DeliveryItem)
                .ThenInclude(di => di!.DeliveryRequest)
                .ThenInclude(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .Where(
                    suh =>
                        suh.StockUpdatedHistoryDetails.FirstOrDefault()!.AidRequestId
                        == aidRequestId
                )
                .ToListAsync();

            stockUpdatedHistories = stockUpdatedHistories
                .Where(
                    suh =>
                        suh.StockUpdatedHistoryDetails.All(suhd => suhd.StockId != null)
                        && (
                            suh.StockUpdatedHistoryDetails.FirstOrDefault()!.DeliveryItemId == null
                            || suh.StockUpdatedHistoryDetails
                                .FirstOrDefault()!
                                .DeliveryItem!.DeliveryRequest.Status
                                == DeliveryRequestStatus.FINISHED
                        )
                )
                .ToList();

            foreach (StockUpdatedHistory suh in stockUpdatedHistories)
            {
                foreach (StockUpdatedHistoryDetail suhd in suh.StockUpdatedHistoryDetails)
                {
                    if (suhd.DeliveryItemId != null)
                    {
                        suhd.DeliveryItem!.DeliveryRequest.ScheduledRouteDeliveryRequests =
                            suhd.DeliveryItem!.DeliveryRequest.ScheduledRouteDeliveryRequests
                                .Where(
                                    srdr =>
                                        srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                                )
                                .ToList();
                    }
                }
            }

            return stockUpdatedHistories;
        }

        public async Task<
            List<StockUpdatedHistory>
        > FindStockUpdatedHistoriesByDonatedRequestIdAsync(Guid donatedRequestId)
        {
            List<StockUpdatedHistory> stockUpdatedHistories = await _context.StockUpdatedHistories
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.DeliveryItem)
                .ThenInclude(di => di!.DeliveryRequest)
                .ThenInclude(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.DeliveryItem)
                .ThenInclude(di => di!.DonatedItem)
                .Where(
                    suh =>
                        suh.StockUpdatedHistoryDetails.Count > 0
                        && suh.StockUpdatedHistoryDetails.FirstOrDefault()!.DeliveryItemId != null
                        && suh.StockUpdatedHistoryDetails
                            .FirstOrDefault()!
                            .DeliveryItem!.DonatedItemId != null
                        && suh.StockUpdatedHistoryDetails
                            .FirstOrDefault()!
                            .DeliveryItem!.DonatedItem!.DonatedRequestId == donatedRequestId
                )
                .ToListAsync();

            foreach (StockUpdatedHistory suh in stockUpdatedHistories)
            {
                foreach (StockUpdatedHistoryDetail suhd in suh.StockUpdatedHistoryDetails)
                {
                    suhd.DeliveryItem!.DeliveryRequest.ScheduledRouteDeliveryRequests =
                        suhd.DeliveryItem!.DeliveryRequest.ScheduledRouteDeliveryRequests
                            .Where(
                                srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                            )
                            .ToList();
                }
            }

            return stockUpdatedHistories;
        }

        public async Task<StockUpdatedHistory?> FindStockUpdatedHistoryByIdAsync(
            Guid stockUpdatedHistoryId
        )
        {
            return await _context.StockUpdatedHistories.FirstOrDefaultAsync(
                suh => suh.Id == stockUpdatedHistoryId
            );
        }

        public async Task<int> UpdateStockUpdatedHistoryAsync(
            StockUpdatedHistory stockUpdatedHistory
        )
        {
            _context.Update(stockUpdatedHistory);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<StockUpdatedHistory?> FindStockUpdatedHistoryByIdAndCharityUnitUserIdAsync(
            Guid stockUpdatedHistoryId,
            Guid? userId
        )
        {
            StockUpdatedHistory? stockUpdatedHistory = await _context.StockUpdatedHistories
                .Include(suh => suh.Branch)
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(suh => suh.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .ThenInclude(s => s!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .FirstOrDefaultAsync(
                    suh =>
                        suh.Id == stockUpdatedHistoryId && suh.StockUpdatedHistoryDetails.Count > 0
                );

            if (stockUpdatedHistory == null)
                return null;

            if (
                stockUpdatedHistory.StockUpdatedHistoryDetails[0].AidRequestId != null
                && stockUpdatedHistory.StockUpdatedHistoryDetails[0].AidRequest!.CharityUnitId
                    != null
                && (
                    userId != null
                        ? stockUpdatedHistory.StockUpdatedHistoryDetails[0]
                            .AidRequest!
                            .CharityUnit!
                            .UserId == userId
                        : true
                )
            )
                return stockUpdatedHistory;
            else
                return null;
        }
    }
}
