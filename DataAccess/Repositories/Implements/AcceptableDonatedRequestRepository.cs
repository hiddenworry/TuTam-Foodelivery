using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class AcceptableDonatedRequestRepository : IAcceptableDonatedRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public AcceptableDonatedRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAcceptableDonatedRequestsAsync(
            List<AcceptableDonatedRequest> acceptableDonatedRequests
        )
        {
            int rs = 0;
            foreach (AcceptableDonatedRequest item in acceptableDonatedRequests)
            {
                rs += await CreateAcceptableDonatedRequestAsync(item);
            }
            return rs;
        }

        public async Task<int> CreateAcceptableDonatedRequestAsync(
            AcceptableDonatedRequest acceptableDonatedRequest
        )
        {
            await _context.AcceptableDonatedRequests.AddAsync(acceptableDonatedRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<AcceptableDonatedRequest?> FindPendingAcceptableDonatedRequestByDonatedRequestIdAndBranchIdAsync(
            Guid donatedRequestId,
            Guid branchId
        )
        {
            return await _context.AcceptableDonatedRequests.FirstOrDefaultAsync(
                adr =>
                    adr.DonatedRequestId == donatedRequestId
                    && adr.BranchId == branchId
                    && adr.Status == AcceptableDonatedRequestStatus.PENDING
            );
        }

        public async Task<int> UpdateAcceptableDonatedRequestAsync(
            AcceptableDonatedRequest pendingAcceptableDonatedRequest
        )
        {
            _context.AcceptableDonatedRequests.Update(pendingAcceptableDonatedRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<
            List<AcceptableDonatedRequest>
        > FindPendingAcceptableDonatedRequestsByDonatedRequestIdAsync(Guid donatedRequestId)
        {
            return await _context.AcceptableDonatedRequests
                .Where(
                    adr =>
                        adr.DonatedRequestId == donatedRequestId
                        && adr.Status == AcceptableDonatedRequestStatus.PENDING
                )
                .ToListAsync();
        }
    }
}
