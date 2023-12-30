using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DataAccess.Repositories.Implements
{
    public class DonatedRequestRepository : IDonatedRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public DonatedRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateDonatedRequestAsync(DonatedRequest donatedRequest)
        {
            await _context.DonatedRequests.AddAsync(donatedRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<DonatedRequest?> FindAcceptedDonatedRequestByIdAndBranchIdAsync(
            Guid id,
            Guid branchId
        )
        {
            DonatedRequest? donatedRequest = await _context.DonatedRequests
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .Include(dr => dr.AcceptableDonatedRequests)
                .FirstOrDefaultAsync(
                    dr => dr.Id == id && dr.Status == DonatedRequestStatus.ACCEPTED
                );

            if (donatedRequest != null)
                donatedRequest.DonatedItems = donatedRequest.DonatedItems
                    .Where(di => di.Status == DonatedItemStatus.ACCEPTED)
                    .ToList();

            return donatedRequest != null
                ? (
                    donatedRequest.AcceptableDonatedRequests.Any(
                        adr =>
                            adr.BranchId == branchId
                            && adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                    )
                        ? donatedRequest
                        : null
                )
                : null;
        }

        public async Task<DonatedRequest?> FindDonatedRequestByIdAsync(Guid id)
        {
            return await _context.DonatedRequests.FirstOrDefaultAsync(dr => dr.Id == id);
        }

        public async Task<DonatedRequest?> FindDonatedRequestByIdForDetailAsync(Guid id)
        {
            return await _context.DonatedRequests
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .Include(dr => dr.User)
                .ThenInclude(u => u.Role)
                .Include(dr => dr.Activity)
                .Include(dr => dr.AcceptableDonatedRequests)
                .ThenInclude(adr => adr.Branch)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemTemplate)
                .ThenInclude(it => it.ItemCategory)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemTemplate.Unit)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemAttributeValues)
                .ThenInclude(itav => itav.AttributeValue)
                .FirstOrDefaultAsync(dr => dr.Id == id);
        }

        public async Task<DonatedRequest?> FindPendingDonatedRequestByIdAsync(Guid id)
        {
            return await _context.DonatedRequests
                .Include(dr => dr.DonatedItems)
                .Include(dr => dr.User)
                .FirstOrDefaultAsync(
                    dr => dr.Id == id && dr.Status == DonatedRequestStatus.PENDING
                );
        }

        public async Task<List<DonatedRequest>> GetDonatedRequestsAsync(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        )
        {
            List<DonatedRequest> donatedRequests = await _context.DonatedRequests
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .Include(dr => dr.AcceptableDonatedRequests)
                .ThenInclude(adr => adr.Branch)
                .Include(dr => dr.User)
                .ThenInclude(u => u.Role)
                .Include(dr => dr.Activity)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemTemplate)
                .ThenInclude(it => it.ItemCategory)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemTemplate.Unit)
                .Include(dr => dr.DonatedItems)
                .ThenInclude(di => di.Item)
                .ThenInclude(it => it.ItemAttributeValues)
                .ThenInclude(itav => itav.AttributeValue)
                .Where(
                    dr =>
                        (userId != null ? dr.UserId == userId : true)
                        && (branchId == null ? (status != null ? dr.Status == status : true) : true)
                        && (activityId != null ? dr.ActivityId == activityId : true)
                )
                .ToListAsync();

            if (branchId != null)
            {
                if (status == null)
                {
                    donatedRequests = donatedRequests
                        .Where(
                            dr =>
                                dr.AcceptableDonatedRequests
                                    .Select(adr => adr.BranchId)
                                    .Contains((Guid)branchId)
                        )
                        .ToList();
                }
                else if (status == DonatedRequestStatus.PENDING)
                {
                    donatedRequests = donatedRequests
                        .Where(
                            dr =>
                                dr.Status == DonatedRequestStatus.PENDING
                                && dr.AcceptableDonatedRequests.Any(
                                    adr =>
                                        adr.BranchId == branchId
                                        && adr.Status == AcceptableDonatedRequestStatus.PENDING
                                )
                        )
                        .ToList();
                }
                else if (status == DonatedRequestStatus.REJECTED)
                {
                    donatedRequests = donatedRequests
                        .Where(
                            dr =>
                                dr.AcceptableDonatedRequests.Any(
                                    adr =>
                                        adr.BranchId == branchId
                                        && adr.Status == AcceptableDonatedRequestStatus.REJECTED
                                )
                        )
                        .ToList();
                }
                else
                {
                    donatedRequests = donatedRequests
                        .Where(
                            dr =>
                                dr.Status == status
                                && dr.AcceptableDonatedRequests.Any(
                                    adr =>
                                        adr.BranchId == branchId
                                        && adr.Status == AcceptableDonatedRequestStatus.ACCEPTED
                                )
                        )
                        .ToList();
                }
            }

            return donatedRequests
                .Where(
                    dr =>
                        JsonConvert.DeserializeObject<List<ScheduledTime>>(dr.ScheduledTimes)
                        != null
                            ? JsonConvert
                                .DeserializeObject<List<ScheduledTime>>(dr.ScheduledTimes)!
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

        public async Task<int> UpdateDonatedRequestAsync(DonatedRequest donatedRequest)
        {
            _context.DonatedRequests.Update(donatedRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> CountDonatedRequestByStatus(
            DonatedRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId,
            Guid? userId,
            Guid? activityId
        )
        {
            var query = _context.DonatedRequests
                .Include(a => a.AcceptableDonatedRequests)
                .AsQueryable();

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
            if (userId != null)
            {
                query = query.Where(a => a.UserId == userId);
            }
            if (userId != null)
            {
                query = query.Where(
                    a =>
                        a.AcceptableDonatedRequests.Any(
                            a =>
                                a.BranchId == branchId
                                && a.Status == AcceptableDonatedRequestStatus.ACCEPTED
                        )
                );
            }
            if (activityId != null)
            {
                query = query.Where(a => a.ActivityId == activityId);
            }
            int count = await query.CountAsync();

            return count;
        }

        public async Task<
            List<DonatedRequest>
        > FindPendingAndAcceptedAndProcessingDonatedRequestsAsync()
        {
            return await _context.DonatedRequests
                .Include(dr => dr.User)
                .Include(dr => dr.AcceptableDonatedRequests)
                .ThenInclude(adr => adr.Branch)
                .Include(dr => dr.DeliveryRequests)
                .ThenInclude(dr => dr.ScheduledRouteDeliveryRequests)
                .Where(
                    dr =>
                        dr.Status == DonatedRequestStatus.PENDING
                        || dr.Status == DonatedRequestStatus.ACCEPTED
                        || dr.Status == DonatedRequestStatus.PROCESSING
                )
                .ToListAsync();
        }
    }
}
