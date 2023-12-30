using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ScheduledRouteRepository : IScheduledRouteRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ScheduledRouteRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddScheduledRouteAsync(ScheduledRoute scheduledRoute)
        {
            await _context.ScheduledRoutes.AddAsync(scheduledRoute);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<
            List<ScheduledRoute>
        > FindAcceptedAndProcessingScheduledRoutesByContributorIdAsync(Guid contributorId)
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .Where(
                    sr =>
                        sr.UserId == contributorId
                        && (
                            sr.Status == ScheduledRouteStatus.ACCEPTED
                            || sr.Status == ScheduledRouteStatus.PROCESSING
                        )
                )
                .ToListAsync();
        }

        public async Task<ScheduledRoute?> FindAcceptedScheduledRouteByIdAndUserIdAsync(
            Guid scheduledRouteId,
            Guid userId
        )
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .FirstOrDefaultAsync(
                    sr =>
                        sr.Id == scheduledRouteId
                        && sr.UserId == userId
                        && sr.Status == ScheduledRouteStatus.ACCEPTED
                );
        }

        public async Task<ScheduledRoute?> FindPendingScheduledRouteByIdAsync(Guid id)
        {
            return await _context.ScheduledRoutes
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .FirstOrDefaultAsync(
                    sr =>
                        sr.Id == id
                        && sr.Status == ScheduledRouteStatus.PENDING
                        && sr.ScheduledRouteDeliveryRequests.Any(
                            srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                        )
                );
        }

        public async Task<ScheduledRoute?> FindProcessingScheduledRouteByIdAndUserIdAsync(
            Guid id,
            Guid userId
        )
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .FirstOrDefaultAsync(
                    sr =>
                        sr.Id == id
                        && sr.UserId == userId
                        && sr.Status == ScheduledRouteStatus.PROCESSING
                );
        }

        public async Task<List<ScheduledRoute>> GetScheduledRoutesForUserAsync(
            ScheduledRouteStatus? status,
            Guid userId
        )
        {
            return await _context.ScheduledRoutes
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .Where(
                    s =>
                        status != null
                            ? (
                                status == ScheduledRouteStatus.PENDING
                                    ? (s.Status == ScheduledRouteStatus.PENDING)
                                    : (s.UserId == userId && s.Status == status)
                            )
                            : (s.UserId == userId && s.Status != ScheduledRouteStatus.PENDING)
                )
                .ToListAsync();
        }

        public async Task<List<ScheduledRoute>> GetScheduledRoutesForAdminAsync(
            ScheduledRouteStatus? status,
            Guid? userId
        )
        {
            List<ScheduledRoute> scheduledRoutes = await _context.ScheduledRoutes
                .Include(sr => sr.User)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .Include(s => s.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .Where(
                    s =>
                        (userId != null ? (s.UserId == userId) : true)
                        && (status != null ? (s.Status == status) : true)
                )
                .ToListAsync();

            foreach (ScheduledRoute scheduledRoute in scheduledRoutes)
            {
                foreach (
                    ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest in scheduledRoute.ScheduledRouteDeliveryRequests
                )
                {
                    if (
                        scheduledRouteDeliveryRequest.DeliveryRequest.AidRequest != null
                        && scheduledRouteDeliveryRequest.DeliveryRequest.AidRequest.BranchId != null
                    )
                    {
                        scheduledRouteDeliveryRequest.DeliveryRequest.AidRequest.Branch =
                            await _context.Branches.FirstOrDefaultAsync(
                                b =>
                                    b.Id
                                    == scheduledRouteDeliveryRequest
                                        .DeliveryRequest
                                        .AidRequest
                                        .BranchId
                            );
                    }
                }
            }

            return scheduledRoutes;
        }

        public async Task<int> UpdateScheduledRouteAsync(ScheduledRoute scheduledRoute)
        {
            _context.ScheduledRoutes.Update(scheduledRoute);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<ScheduledRoute?> FindScheduledRouteByIdForDetailAsync(Guid id)
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .ThenInclude(b => b.BranchAdmin)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .ThenInclude(b => b!.BranchAdmin)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.DonatedItem)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.AidItem)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        public async Task<ScheduledRoute?> FindScheduledRouteByDeliveryRequestId(
            Guid deliveryRequestId
        )
        {
            var query = _context.ScheduledRoutes
                .Include(a => a.ScheduledRouteDeliveryRequests)
                .Where(
                    a =>
                        a.ScheduledRouteDeliveryRequests.All(
                            p =>
                                p.DeliveryRequestId == deliveryRequestId
                                && p.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED
                        )
                        && a.UserId != null
                        && a.Status != ScheduledRouteStatus.PENDING
                );

            var result = await query.FirstOrDefaultAsync();

            return result;
        }

        public async Task<ScheduledRoute?> FindProcessingScheduledRouteByIdAsync(Guid id)
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.Activity)
                .ThenInclude(a => a!.TargetProcesses)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(sr => sr!.User)
                .FirstOrDefaultAsync(
                    sr => sr.Id == id && sr.Status == ScheduledRouteStatus.PROCESSING
                );
        }

        public async Task<ScheduledRoute?> FindScheduledRouteByIdForDetailForAdminAsync(
            Guid scheduledRouteId,
            bool isProcessing
        )
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .ThenInclude(b => b.BranchAdmin)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .ThenInclude(b => b!.BranchAdmin)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.DonatedItem)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.AidItem)
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .ThenInclude(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .FirstOrDefaultAsync(
                    sr =>
                        sr.Id == scheduledRouteId
                        && (isProcessing ? sr.Status == ScheduledRouteStatus.PROCESSING : true)
                );
        }

        public async Task<ScheduledRoute?> FindAcceptedAndProcessingScheduledRouteByUserIdAsync(
            Guid scheduledRouteId,
            Guid userId
        )
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .FirstOrDefaultAsync(
                    sr =>
                        sr.Id == scheduledRouteId
                        && sr.UserId == userId
                        && (
                            sr.Status == ScheduledRouteStatus.ACCEPTED
                            || sr.Status == ScheduledRouteStatus.PROCESSING
                        )
                );
        }

        public async Task<List<ScheduledRoute>?> FindAcceptedAndProcessingScheduledRoutedAsync()
        {
            return await _context.ScheduledRoutes
                .Include(sr => sr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.DeliveryRequest)
                .Where(
                    sr =>
                        sr.Status == ScheduledRouteStatus.ACCEPTED
                        || sr.Status == ScheduledRouteStatus.PROCESSING
                )
                .ToListAsync();
        }
    }
}
