using DataAccess.DbContextData;
using DataAccess.Entities;

namespace DataAccess.Repositories.Implements
{
    public class ScheduledRouteDeliveryRequestRepository : IScheduledRouteDeliveryRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ScheduledRouteDeliveryRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddScheduledRouteDeliveryRequestsAsync(
            List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests
        )
        {
            int rs = 0;
            foreach (ScheduledRouteDeliveryRequest item in scheduledRouteDeliveryRequests)
            {
                rs += await AddScheduledRouteDeliveryRequestAsync(item);
            }
            return rs;
        }

        public async Task<int> AddScheduledRouteDeliveryRequestAsync(
            ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest
        )
        {
            await _context.ScheduledRouteDeliveryRequests.AddAsync(scheduledRouteDeliveryRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> UpdateScheduledRouteDeliveryRequestsAsync(
            List<ScheduledRouteDeliveryRequest> scheduledRouteDeliveryRequests
        )
        {
            int rs = 0;
            foreach (ScheduledRouteDeliveryRequest item in scheduledRouteDeliveryRequests)
            {
                rs += await UpdateScheduledRouteDeliveryRequestAsync(item);
            }
            return rs;
        }

        public async Task<int> UpdateScheduledRouteDeliveryRequestAsync(
            ScheduledRouteDeliveryRequest scheduledRouteDeliveryRequest
        )
        {
            _context.ScheduledRouteDeliveryRequests.Update(scheduledRouteDeliveryRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }
    }
}
