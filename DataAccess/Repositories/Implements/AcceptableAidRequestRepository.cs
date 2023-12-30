using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class AcceptableAidRequestRepository : IAcceptableAidRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public AcceptableAidRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAcceptableAidRequestsAsync(
            List<AcceptableAidRequest> acceptableAidRequests
        )
        {
            int rs = 0;
            foreach (AcceptableAidRequest item in acceptableAidRequests)
            {
                rs += await CreateAcceptableAidRequestAsync(item);
            }
            return rs;
        }

        public async Task<int> CreateAcceptableAidRequestAsync(AcceptableAidRequest item)
        {
            await _context.AcceptableAidRequests.AddAsync(item);
            return (await _context.SaveChangesAsync()) > 0 ? 1 : 0;
        }

        public async Task<AcceptableAidRequest?> FindPendingAcceptableAidRequestByAidRequestIdAndBranchIdAsync(
            Guid aidRequestId,
            Guid branchId
        )
        {
            return await _context.AcceptableAidRequests.FirstOrDefaultAsync(
                aar =>
                    aar.AidRequestId == aidRequestId
                    && aar.BranchId == branchId
                    && aar.Status == AcceptableAidRequestStatus.PENDING
            );
        }

        public async Task<int> UpdateAcceptableAidRequestAsync(
            AcceptableAidRequest acceptableAidRequest
        )
        {
            _context.Update(acceptableAidRequest);
            return (await _context.SaveChangesAsync()) > 0 ? 1 : 0;
        }

        public async Task<
            List<AcceptableAidRequest>
        > FindPendingAcceptableAidRequestsByAidRequestIdAsync(Guid aidRequestId)
        {
            return await _context.AcceptableAidRequests
                .Where(
                    aar =>
                        aar.AidRequestId == aidRequestId
                        && aar.Status == AcceptableAidRequestStatus.PENDING
                )
                .ToListAsync();
        }
    }
}
