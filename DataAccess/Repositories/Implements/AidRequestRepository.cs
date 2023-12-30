using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DataAccess.Repositories.Implements
{
    public class AidRequestRepository : IAidRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public AidRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAidRequestAsync(AidRequest aidRequest)
        {
            await _context.AidRequests.AddAsync(aidRequest);
            return (await _context.SaveChangesAsync()) > 0 ? 1 : 0;
        }

        public async Task<AidRequest?> FindAcceptedOrProcessingAidRequestByIdAndBranchIdAsync(
            Guid id,
            Guid branchId
        )
        {
            AidRequest? aidRequest = await _context.AidRequests
                .Include(ai => ai.AidItems)
                .ThenInclude(ai => ai.Item)
                .Include(ai => ai.AcceptableAidRequests)
                .FirstOrDefaultAsync(
                    ar =>
                        ar.Id == id
                        && (
                            ar.Status == AidRequestStatus.ACCEPTED
                            || ar.Status == AidRequestStatus.PROCESSING
                        )
                );

            if (aidRequest != null)
                aidRequest.AidItems = aidRequest.AidItems
                    .Where(
                        ai =>
                            ai.Status == AidItemStatus.ACCEPTED
                            || ai.Status == AidItemStatus.APPLIED_TO_ACTIVITY
                    )
                    .ToList();

            return aidRequest != null
                ? (
                    aidRequest.AcceptableAidRequests.Any(
                        aar =>
                            aar.BranchId == branchId
                            && aar.Status == AcceptableAidRequestStatus.ACCEPTED
                    )
                        ? aidRequest
                        : null
                )
                : null;
        }

        public async Task<AidRequest?> FindAidRequestByIdAsync(Guid id)
        {
            return await _context.AidRequests
                .Include(ar => ar.CharityUnit)
                .FirstOrDefaultAsync(ar => ar.Id == id);
        }

        public async Task<AidRequest?> FindAidRequestOfCharityUnitByIdForDetailAsync(Guid id)
        {
            AidRequest? aidRequest = await _context.AidRequests
                .Include(ar => ar.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .Include(ar => ar.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.StockUpdatedHistory)
                .Include(ar => ar.AidItems)
                .ThenInclude(di => di.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .Include(ar => ar.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(ar => ar.AcceptableAidRequests)
                .ThenInclude(aar => aar.Branch)
                .FirstOrDefaultAsync(ar => ar.Id == id && ar.CharityUnitId != null);

            if (aidRequest == null)
                return null;

            if (aidRequest.CharityUnit != null)
                aidRequest.CharityUnit.Charity = (
                    await _context.Charities.FirstOrDefaultAsync(
                        c => c.Id == aidRequest.CharityUnit.CharityId
                    )
                )!;

            return aidRequest;
        }

        public async Task<AidRequest?> FindPendingAidRequestByIdAsync(Guid id)
        {
            return await _context.AidRequests
                .Include(ar => ar.AidItems)
                .Include(ar => ar.CharityUnit)
                .FirstOrDefaultAsync(ar => ar.Id == id && ar.Status == AidRequestStatus.PENDING);
        }

        public async Task<List<AidRequest>> GetAidRequestsOfCharityUnitAsync(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId
        )
        {
            List<AidRequest> aidRequests = await _context.AidRequests
                .Include(ar => ar.AcceptableAidRequests)
                .ThenInclude(aar => aar.Branch)
                .Include(ar => ar.CharityUnit)
                .Where(
                    ar =>
                        (charityUnitId != null ? ar.CharityUnitId == charityUnitId : true)
                        && (branchId == null ? (status != null ? ar.Status == status : true) : true)
                        && ar.CharityUnitId != null
                )
                .ToListAsync();

            if (branchId != null)
            {
                if (status == null)
                {
                    aidRequests = aidRequests
                        .Where(
                            ar =>
                                ar.AcceptableAidRequests
                                    .Select(aar => aar.BranchId)
                                    .Contains((Guid)branchId)
                        )
                        .ToList();
                }
                else if (status == AidRequestStatus.PENDING)
                {
                    aidRequests = aidRequests
                        .Where(
                            ar =>
                                ar.Status == AidRequestStatus.PENDING
                                && ar.AcceptableAidRequests.Any(
                                    aar =>
                                        aar.BranchId == branchId
                                        && aar.Status == AcceptableAidRequestStatus.PENDING
                                )
                        )
                        .ToList();
                }
                else if (status == AidRequestStatus.REJECTED)
                {
                    aidRequests = aidRequests
                        .Where(
                            ar =>
                                ar.AcceptableAidRequests.Any(
                                    aar =>
                                        aar.BranchId == branchId
                                        && aar.Status == AcceptableAidRequestStatus.REJECTED
                                )
                        )
                        .ToList();
                }
                else
                {
                    aidRequests = aidRequests
                        .Where(
                            ar =>
                                ar.Status == status
                                && ar.AcceptableAidRequests.Any(
                                    aar =>
                                        aar.BranchId == branchId
                                        && aar.Status == AcceptableAidRequestStatus.ACCEPTED
                                )
                        )
                        .ToList();
                }
            }

            return aidRequests
                .Where(
                    ar =>
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(ar.ScheduledTimes)
                        != null
                            ? JsonConvert
                                .DeserializeObject<List<ScheduledTime>>(ar.ScheduledTimes)!
                                .Any(
                                    st =>
                                        (
                                            startDate != null
                                                ? DateTime.Parse(st.Day) >= startDate
                                                : true
                                        )
                                        && (
                                            endDate != null
                                                ? DateTime.Parse(st.Day) <= endDate
                                                : true
                                        )
                                )
                            : true
                )
                .ToList();
        }

        public async Task<
            List<AidRequest>
        > GetAidRequestsOfCharityUnitWhichHasAppliableItemsToActivityAsync(
            DateTime? startDate,
            DateTime? endDate,
            Guid branchId,
            Guid? charityUnitId
        )
        {
            List<AidRequest> aidRequests = await _context.AidRequests
                .Include(ar => ar.AcceptableAidRequests)
                .ThenInclude(aar => aar.Branch)
                .Include(ar => ar.CharityUnit)
                .Include(ar => ar.AidItems)
                .Where(ar => charityUnitId != null ? ar.CharityUnitId == charityUnitId : true)
                .ToListAsync();

            aidRequests = aidRequests
                .Where(
                    ar =>
                        (
                            ar.Status == AidRequestStatus.ACCEPTED
                            || ar.Status == AidRequestStatus.PROCESSING
                        )
                        && ar.AcceptableAidRequests.Any(
                            aar =>
                                aar.BranchId == branchId
                                && aar.Status == AcceptableAidRequestStatus.ACCEPTED
                        )
                        && ar.CharityUnitId != null
                )
                .ToList();

            return aidRequests
                .Where(
                    ar =>
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(ar.ScheduledTimes)
                        != null
                            ? JsonConvert
                                .DeserializeObject<List<ScheduledTime>>(ar.ScheduledTimes)!
                                .Any(
                                    st =>
                                        (
                                            startDate != null
                                                ? DateTime.Parse(st.Day) >= startDate
                                                : true
                                        )
                                        && (
                                            endDate != null
                                                ? DateTime.Parse(st.Day) <= endDate
                                                : true
                                        )
                                )
                            : true
                )
                .ToList();
        }

        public async Task<int> UpdateAidRequestAsync(AidRequest aidRequest)
        {
            _context.AidRequests.Update(aidRequest);
            return (await _context.SaveChangesAsync()) > 0 ? 1 : 0;
        }

        public async Task<int> CountAidRequestByStatus(
            AidRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? charityUnitId
        )
        {
            var query = _context.AidRequests.Include(a => a.AcceptableAidRequests).AsQueryable();

            if (status != null)
            {
                // Filter by status
                query = query.Where(a => a.Status == status);
            }

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.CreatedDate >= startDate);
            }

            if (endDate != null && startDate == null)
            {
                query = query.Where(a => a.CreatedDate <= endDate);
            }
            if (endDate != null && startDate != null)
            {
                query = query.Where(a => a.CreatedDate <= endDate && a.CreatedDate >= startDate);
            }
            if (branchId != null)
            {
                query = query.Where(
                    a =>
                        a.AcceptableAidRequests.Any(
                            a =>
                                a.BranchId == branchId
                                && a.Status == AcceptableAidRequestStatus.ACCEPTED
                        )
                );
            }
            if (charityUnitId != null)
            {
                query = query.Where(a => a.CharityUnitId == charityUnitId);
            }
            int count = await query.CountAsync();

            return count;
        }

        public async Task<int> CountAidRequestBySelfShippingFlag(
            bool? isSelfShipping,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var query = _context.AidRequests.AsQueryable();

            if (isSelfShipping != null)
            {
                query = query.Where(a => a.IsSelfShipping == isSelfShipping);
            }

            if (startDate != null && endDate == null)
            {
                query = query.Where(a => a.CreatedDate >= startDate);
            }

            if (endDate != null && startDate == null)
            {
                query = query.Where(a => a.CreatedDate <= endDate);
            }
            if (endDate != null && startDate != null)
            {
                query = query.Where(a => a.CreatedDate <= endDate && a.CreatedDate >= startDate);
            }

            int count = await query.CountAsync();

            return count;
        }

        public async Task<AidRequest?> FindAcceptedAndProcessingAidRequestByIdAndBranchIdToFinishAsync(
            Guid id,
            Guid branchId
        )
        {
            AidRequest? aidRequest = await _context.AidRequests
                .Include(ar => ar.CharityUnit)
                .Include(ar => ar.Branch)
                .Include(ai => ai.AcceptableAidRequests)
                .FirstOrDefaultAsync(
                    ar =>
                        ar.Id == id
                        && (
                            ar.Status == AidRequestStatus.PROCESSING
                            || ar.Status == AidRequestStatus.ACCEPTED
                        )
                );

            return aidRequest != null
                ? (
                    aidRequest.AcceptableAidRequests.Any(
                        aar =>
                            aar.BranchId == branchId
                            && aar.Status == AcceptableAidRequestStatus.ACCEPTED
                    )
                        ? aidRequest
                        : null
                )
                : null;
        }

        public async Task<List<AidRequest>> FindPendingAndAcceptedAndProcessingAidRequestsAsync()
        {
            return await _context.AidRequests
                .Include(dr => dr.CharityUnit)
                .ThenInclude(a => a!.User)
                .Include(dr => dr.AcceptableAidRequests)
                .ThenInclude(adr => adr.Branch)
                .Include(dr => dr.DeliveryRequests)
                .ThenInclude(dr => dr.ScheduledRouteDeliveryRequests)
                .Where(
                    dr =>
                        dr.Status == AidRequestStatus.PENDING
                        || dr.Status == AidRequestStatus.ACCEPTED
                        || dr.Status == AidRequestStatus.PROCESSING
                )
                .ToListAsync();
        }
    }
}
