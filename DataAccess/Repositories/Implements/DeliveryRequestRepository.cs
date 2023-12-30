using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.ModelsEnum;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class DeliveryRequestRepository : IDeliveryRequestRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public DeliveryRequestRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckContributorAvailabileToCancelCollaborator(Guid userId)
        {
            return !await _context.DeliveryRequests
                .Include(d => d.ScheduledRouteDeliveryRequests)
                .ThenInclude(d => d.ScheduledRoute) // Explicitly include ScheduledRoute
                .AnyAsync(
                    d =>
                        (
                            d.ScheduledRouteDeliveryRequests.Any(
                                srdr => srdr.ScheduledRoute.UserId == userId
                            )
                            && (
                                d.Status == DeliveryRequestStatus.SHIPPING
                                || d.Status == DeliveryRequestStatus.DELIVERED
                                || d.Status == DeliveryRequestStatus.ARRIVED_DELIVERY
                                || d.Status == DeliveryRequestStatus.ARRIVED_PICKUP
                                || d.Status == DeliveryRequestStatus.ACCEPTED
                                || d.Status == DeliveryRequestStatus.COLLECTED
                            )
                        )
                );
        }

        public async Task<int> CreateDeliveryRequestAsync(DeliveryRequest deliveryRequest)
        {
            await _context.DeliveryRequests.AddAsync(deliveryRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        //public async Task<
        //    List<DeliveryRequest>
        //> FindPendingDeliveryRequestsByDeliveryTypeAndMainBranchIdAsync(
        //    DeliveryType deliveryType,
        //    Guid? branchId
        //)
        //{
        //    if (deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
        //    {
        //        return await _context.DeliveryRequests
        //            .Include(dr => dr.ScheduledRouteDeliveryRequests)
        //            .ThenInclude(srdr => srdr.ScheduledRoute)
        //            .Include(dr => dr.DeliveryItems)
        //            .Include(dr => dr.Branch)
        //            .Include(dr => dr.DonatedRequest)
        //            .Where(
        //                dr =>
        //                    dr.Status == DeliveryRequestStatus.PENDING
        //                    && dr.DonatedRequest != null
        //                    && (branchId == null ? true : dr.BranchId == branchId)
        //            )
        //            .ToListAsync();
        //    }
        //    else if (deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
        //    {
        //        List<DeliveryRequest> deliveryRequests = await _context.DeliveryRequests
        //            .Include(dr => dr.ScheduledRouteDeliveryRequests)
        //            .ThenInclude(srdr => srdr.ScheduledRoute)
        //            .Include(dr => dr.DeliveryItems)
        //            .Include(dr => dr.Branch)
        //            .Include(dr => dr.AidRequest)
        //            .ThenInclude(ar => ar!.CharityUnit)
        //            .Where(
        //                dr =>
        //                    dr.Status == DeliveryRequestStatus.PENDING
        //                    && dr.AidRequest != null
        //                    && dr.AidRequest.CharityUnitId != null
        //                    && (branchId == null ? true : dr.BranchId == branchId)
        //            )
        //            .ToListAsync();

        //        return deliveryRequests;
        //    }
        //    else
        //    {
        //        List<DeliveryRequest> deliveryRequests = await _context.DeliveryRequests
        //            .Include(dr => dr.ScheduledRouteDeliveryRequests)
        //            .ThenInclude(srdr => srdr.ScheduledRoute)
        //            .Include(dr => dr.DeliveryItems)
        //            .Include(dr => dr.Branch)
        //            .Include(dr => dr.AidRequest)
        //            .ThenInclude(ar => ar!.Branch)
        //            .Where(
        //                dr =>
        //                    dr.Status == DeliveryRequestStatus.PENDING
        //                    && dr.AidRequest != null
        //                    && dr.AidRequest.BranchId != null
        //                    && dr.AidRequest.Branch != null
        //                    && (branchId == null ? true : dr.AidRequest.BranchId == branchId)
        //            )
        //            .ToListAsync();

        //        return deliveryRequests;
        //    }
        //}

        private Guid GetMainBranchId(DeliveryRequest deliveryRequest)
        {
            if (deliveryRequest.DonatedRequestId != null)
            {
                return deliveryRequest.BranchId;
            }
            else
            {
                if (deliveryRequest.AidRequest!.CharityUnitId != null)
                {
                    return deliveryRequest.BranchId;
                }
                else
                {
                    return (Guid)deliveryRequest.AidRequest.BranchId!;
                }
            }
        }

        public async Task<
            List<List<DeliveryRequest>>
        > FindPendingDeliveryRequestsByDeliveryTypeAndMainBranchIdAsync(
            DeliveryType? deliveryType,
            Guid? branchId
        )
        {
            List<List<DeliveryRequest>> rs = new();
            List<DeliveryRequest> deliveryRequestsTypeDonatedRequestToBranch = new();
            List<DeliveryRequest> deliveryRequestsTypeBranchToAidRequest = new();
            List<DeliveryRequest> deliveryRequestsTypeBranchToBranch = new();

            if (deliveryType == null || deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
            {
                deliveryRequestsTypeDonatedRequestToBranch = await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .ThenInclude(di => di.DonatedItem)
                    .ThenInclude(di => di!.Item)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.DonatedRequest)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.DonatedRequest != null
                            && (branchId == null ? true : dr.BranchId == branchId)
                    )
                    .ToListAsync();
            }

            if (deliveryType == null || deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
            {
                deliveryRequestsTypeBranchToAidRequest = await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .ThenInclude(di => di.AidItem)
                    .ThenInclude(ai => ai!.Item)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.AidRequest)
                    .ThenInclude(ar => ar!.CharityUnit)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.AidRequest != null
                            && dr.AidRequest.CharityUnitId != null
                            && (branchId == null ? true : dr.BranchId == branchId)
                    )
                    .ToListAsync();
            }

            if (deliveryType == null || deliveryType == DeliveryType.BRANCH_TO_BRANCH)
            {
                deliveryRequestsTypeBranchToBranch = await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .ThenInclude(di => di.AidItem)
                    .ThenInclude(ai => ai!.Item)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.AidRequest)
                    .ThenInclude(ar => ar!.Branch)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.AidRequest != null
                            && dr.AidRequest.BranchId != null
                            && dr.AidRequest.Branch != null
                            && (branchId == null ? true : dr.AidRequest.BranchId == branchId)
                    )
                    .ToListAsync();
            }

            deliveryRequestsTypeDonatedRequestToBranch.AddRange(deliveryRequestsTypeBranchToBranch);

            rs.AddRange(
                deliveryRequestsTypeDonatedRequestToBranch
                    .GroupBy(dr => GetMainBranchId(dr))
                    .Select(g => g.ToList())
            );
            rs.AddRange(
                deliveryRequestsTypeBranchToAidRequest.Select(
                    dr => new List<DeliveryRequest> { dr }
                )
            );

            return rs;
        }

        public async Task<
            List<DeliveryRequest>
        > FindPendingDeliveryRequestsByDeliveryTypeAndMainBranchIdAsync(
            DeliveryType deliveryType,
            Guid? branchId
        )
        {
            if (deliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
            {
                return await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.DonatedRequest)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.DonatedRequest != null
                            && (branchId == null ? true : dr.BranchId == branchId)
                    )
                    .ToListAsync();
            }
            else if (deliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
            {
                List<DeliveryRequest> deliveryRequests = await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.AidRequest)
                    .ThenInclude(ar => ar!.CharityUnit)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.AidRequest != null
                            && dr.AidRequest.CharityUnitId != null
                            && (branchId == null ? true : dr.BranchId == branchId)
                    )
                    .ToListAsync();

                return deliveryRequests;
            }
            else
            {
                List<DeliveryRequest> deliveryRequests = await _context.DeliveryRequests
                    .Include(dr => dr.ScheduledRouteDeliveryRequests)
                    .ThenInclude(srdr => srdr.ScheduledRoute)
                    .Include(dr => dr.DeliveryItems)
                    .Include(dr => dr.Branch)
                    .Include(dr => dr.AidRequest)
                    .ThenInclude(ar => ar!.Branch)
                    .Where(
                        dr =>
                            dr.Status == DeliveryRequestStatus.PENDING
                            && dr.AidRequest != null
                            && dr.AidRequest.BranchId != null
                            && dr.AidRequest.Branch != null
                            && (branchId == null ? true : dr.AidRequest.BranchId == branchId)
                    )
                    .ToListAsync();

                return deliveryRequests;
            }
        }

        public async Task<int> UpdateDeliveryRequestsAsync(List<DeliveryRequest> deliveryRequests)
        {
            int rs = 0;
            foreach (DeliveryRequest deliveryRequest in deliveryRequests)
            {
                rs += await UpdateDeliveryRequestAsync(deliveryRequest);
            }
            return rs;
        }

        public async Task<int> UpdateDeliveryRequestAsync(DeliveryRequest deliveryRequest)
        {
            _context.DeliveryRequests.Update(deliveryRequest);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<List<DeliveryRequest>?> GetDeliveryRequestsAsync(
            DeliveryFilterRequest request
        )
        {
            var query = _context.DeliveryRequests
                .Include(a => a.Branch)
                .ThenInclude(a => a.BranchAdmin)
                .Include(a => a.AidRequest)
                .ThenInclude(a => a!.CharityUnit)
                .ThenInclude(a => a!.User)
                .Include(a => a.DonatedRequest)
                .ThenInclude(a => a!.User)
                .ThenInclude(u => u.CollaboratorApplication)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.AidItem)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.DonatedItem)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.AidItem)
                .AsQueryable();
            if (request.BranchAdminId != null)
            {
                query = query.Where(
                    a =>
                        a.Branch.BranchAdminId == request.BranchAdminId
                        || a.AidRequest != null
                            && a.AidRequest.Branch != null
                            && a.AidRequest.Branch.BranchAdminId == request.BranchAdminId
                );
            }
            if (request.StartDate != null && request.EndDate == null)
            {
                query = query.Where(a => a.CreatedDate >= request.StartDate);
            }
            else if (request.StartDate == null && request.EndDate != null)
            {
                query = query.Where(a => a.CreatedDate <= request.EndDate);
            }
            else if (request.StartDate != null && request.EndDate != null)
            {
                query = query.Where(
                    a => a.CreatedDate >= request.StartDate && a.CreatedDate <= request.EndDate
                );
            }
            if (request.ItemId != null)
            {
                query = query.Where(
                    a =>
                        a.DeliveryItems.Any(
                            a =>
                                (a.DonatedItem != null && a.DonatedItem.ItemId == request.ItemId)
                                || (a.AidItem != null && a.AidItem.ItemId == request.ItemId)
                        )
                );
            }
            if (request.DeliveryType != null)
            {
                if (request.DeliveryType == DeliveryType.DONATED_REQUEST_TO_BRANCH)
                {
                    query = query.Where(a => a.DonatedRequestId != null);
                }
                else if (request.DeliveryType == DeliveryType.BRANCH_TO_AID_REQUEST)
                {
                    query = query.Where(
                        a => a.AidRequest != null && a.AidRequest.CharityUnitId != null
                    );
                }
                else if (request.DeliveryType == DeliveryType.BRANCH_TO_BRANCH)
                {
                    query = query.Where(a => a.AidRequest != null && a.AidRequest.BranchId != null);
                }
            }
            if (request.Status != null)
            {
                query = query.Where(a => a.Status == request.Status);
            }
            if (!string.IsNullOrEmpty(request.KeyWord))
            {
                query = query.Where(
                    a =>
                        (a.Branch != null && a.Branch.Name.ToLower().Contains(request.KeyWord))
                        || (
                            a.DonatedRequest != null
                            && a.DonatedRequest.User != null
                            && (
                                a.DonatedRequest.User.Name != null
                                && a.DonatedRequest.User.Name.ToLower().Contains(request.KeyWord)
                            )
                        )
                        || (
                            a.AidRequest != null
                            && a.AidRequest.CharityUnit != null
                            && a.AidRequest.CharityUnit.Name.ToLower().Contains(request.KeyWord)
                        )
                        || (
                            a.AidRequest != null
                            && a.AidRequest.Branch != null
                            && a.AidRequest.Branch.Name.ToLower().Contains(request.KeyWord)
                        )
                );
            }
            if (!string.IsNullOrEmpty(request.Address))
            {
                query = query.Where(
                    a =>
                        (a.Branch != null && a.Branch.Address.ToLower().Contains(request.Address))
                        || (
                            a.DonatedRequest != null
                            && a.DonatedRequest.User != null
                            && a.DonatedRequest.User.Address != null
                            && a.DonatedRequest.User.Address.ToLower().Contains(request.Address)
                        )
                        || (
                            a.AidRequest != null
                            && a.AidRequest.CharityUnit != null
                            && a.AidRequest.CharityUnit.Address.ToLower().Contains(request.Address)
                        )
                        || (
                            a.AidRequest != null
                            && a.AidRequest.Branch != null
                            && a.AidRequest.Branch.Address.ToLower().Contains(request.Address)
                        )
                );
            }
            if (request.BranchId != null)
            {
                query = query.Where(
                    a =>
                        (a.Branch != null && a.Branch.Id == request.BranchId)
                        || (
                            a.AidRequest != null
                            && a.AidRequest.Branch != null
                            && a.AidRequest.Branch.Id == request.BranchId
                        )
                );
            }
            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }

        public async Task<DeliveryRequest?> GetDeliveryRequestsDetailAsync(
            Guid deliveryRequestId,
            Guid? branchAdminId
        )
        {
            var query = _context.DeliveryRequests
                .Include(a => a.Branch)
                .ThenInclude(a => a.BranchAdmin)
                .Include(a => a.AidRequest)
                .ThenInclude(a => a!.CharityUnit)
                .ThenInclude(a => a!.User)
                .Include(a => a.DonatedRequest)
                .ThenInclude(a => a!.User)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.AidItem)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.DonatedItem)
                .AsQueryable();
            if (branchAdminId != null)
            {
                query = query.Where(
                    a =>
                        a.Branch.BranchAdminId == branchAdminId
                        || a.AidRequest != null
                            && (
                                a.AidRequest.Branch != null
                                && a.AidRequest.Branch.BranchAdminId == branchAdminId
                            )
                );
            }

            return await query.Where(a => a.Id == deliveryRequestId).FirstOrDefaultAsync();
        }

        public async Task<DeliveryRequest?> FindProcessingDeliveryRequestByIdAsync(
            Guid deliveryRequestId
        )
        {
            return await _context.DeliveryRequests
                .Include(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .Include(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(dr => dr.DeliveryItems)
                .Include(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(dr => dr.Branch)
                .FirstOrDefaultAsync(
                    dr =>
                        dr.Id == deliveryRequestId
                        && dr.Status != DeliveryRequestStatus.PENDING
                        && dr.Status != DeliveryRequestStatus.ACCEPTED
                        && dr.Status != DeliveryRequestStatus.REPORTED
                        && dr.Status != DeliveryRequestStatus.FINISHED
                        && dr.Status != DeliveryRequestStatus.EXPIRED
                        && dr.Status != DeliveryRequestStatus.CANCELED
                );
        }

        public async Task<List<DeliveryRequest>?> GetDeliveryRequestsDetailForContributorAsync(
            Guid contributorId,
            Guid deliveryId
        )
        {
            var query = _context.DeliveryRequests
                .Include(a => a.Branch)
                .ThenInclude(a => a.BranchAdmin)
                .Include(a => a.AidRequest)
                .ThenInclude(a => a!.CharityUnit)
                .ThenInclude(a => a!.User)
                .Include(a => a.DonatedRequest)
                .ThenInclude(a => a!.User)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.AidItem)
                .Include(a => a.DeliveryItems)
                .ThenInclude(a => a.DonatedItem)
                .Include(a => a.ScheduledRouteDeliveryRequests)
                .ThenInclude(a => a.ScheduledRoute)
                .AsQueryable();

            query = query.Where(
                a =>
                    a.ScheduledRouteDeliveryRequests.Any(
                        sr =>
                            sr.ScheduledRoute.UserId == contributorId
                            && sr.ScheduledRoute.Status != ScheduledRouteStatus.PENDING
                    )
            );
            var a = query.Count();
            query = query.Where(a => a.Id == deliveryId);
            var b = query.Count();
            return await query.ToListAsync();
        }

        public async Task<DeliveryRequest?> FindDeliveryRequestByIdAsync(Guid deliveryRequestId)
        {
            DeliveryRequest? deliveryRequest = await _context.DeliveryRequests
                .Include(dr => dr.Branch)
                .Include(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .Include(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.Branch)
                .Include(dr => dr.DonatedRequest)
                .Include(dr => dr.Branch)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.StockUpdatedHistory)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .FirstOrDefaultAsync(dr => dr.Id == deliveryRequestId);

            if (deliveryRequest != null)
                deliveryRequest.ScheduledRouteDeliveryRequests =
                    deliveryRequest.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

            return deliveryRequest;
        }

        public async Task<int> CountDeliveryRequest(
            DeliveryRequestStatus? status,
            DateTime? startDate,
            DateTime? endDate,
            Guid? branchId
        )
        {
            var query = _context.DeliveryRequests.AsQueryable();

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
                // Filter by status
                query = query.Where(a => a.BranchId == branchId);
            }
            int count = await query.CountAsync();

            return count;
        }

        public async Task<List<DeliveryRequest>> FindDeliveryRequestsByDonatedRequestIdAsync(
            Guid donatedRequestId
        )
        {
            List<DeliveryRequest> deliveryRequests = await _context.DeliveryRequests
                .Include(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.Report)
                .Where(dr => dr.DonatedRequestId == donatedRequestId)
                .ToListAsync();

            deliveryRequests.ForEach(
                dr =>
                    dr.ScheduledRouteDeliveryRequests = dr.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList()
            );

            return deliveryRequests;
        }

        public async Task<DeliveryRequest?> FindFinishedDeliveryRequestForDetailByIdAndDonorIdAsync(
            Guid deliveryRequestId,
            Guid userId
        )
        {
            DeliveryRequest? deliveryRequest = await _context.DeliveryRequests
                .Include(dr => dr.Branch)
                .Include(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.Activity)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.StockUpdatedHistory)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .Include(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.DonatedItem)
                .ThenInclude(di => di!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.DonatedItem)
                .ThenInclude(di => di!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .FirstOrDefaultAsync(
                    dr =>
                        dr.Id == deliveryRequestId
                        && dr.DonatedRequestId != null
                        && dr.DonatedRequest!.UserId == userId
                        && dr.Status == DeliveryRequestStatus.FINISHED
                );

            if (deliveryRequest != null)
                deliveryRequest.ScheduledRouteDeliveryRequests =
                    deliveryRequest.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();

            return deliveryRequest;
        }

        public async Task<DeliveryRequest?> FindFinishedDeliveryRequestForDetailByIdAndUserIOfCharityUnitAsync(
            Guid deliveryRequestId,
            Guid? userId
        )
        {
            DeliveryRequest? deliveryRequest = await _context.DeliveryRequests
                .Include(dr => dr.Branch)
                .Include(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.StockUpdatedHistory)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.StockUpdatedHistoryDetails)
                .ThenInclude(suhd => suhd.Stock)
                .Include(dr => dr.ScheduledRouteDeliveryRequests)
                .ThenInclude(srdr => srdr.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.AidItem)
                .ThenInclude(di => di!.Item)
                .ThenInclude(i => i.ItemAttributeValues)
                .ThenInclude(iav => iav.AttributeValue)
                .Include(dr => dr.DeliveryItems)
                .ThenInclude(di => di.AidItem)
                .ThenInclude(di => di!.Item)
                .ThenInclude(i => i.ItemTemplate)
                .ThenInclude(it => it.Unit)
                .FirstOrDefaultAsync(
                    dr =>
                        dr.Id == deliveryRequestId
                        && dr.AidRequestId != null
                        && dr.AidRequest!.CharityUnitId != null
                        && (userId != null ? dr.AidRequest!.CharityUnit!.UserId == userId : true)
                        && dr.Status == DeliveryRequestStatus.FINISHED
                );

            if (deliveryRequest != null)
            {
                if (
                    !deliveryRequest.DeliveryItems.All(
                        di => di.StockUpdatedHistoryDetails.All(suhd => suhd.StockId != null)
                    )
                )
                    return null;

                deliveryRequest.ScheduledRouteDeliveryRequests =
                    deliveryRequest.ScheduledRouteDeliveryRequests
                        .Where(srdr => srdr.Status == ScheduledRouteDeliveryRequestStatus.SCHEDULED)
                        .ToList();
            }

            return deliveryRequest;
        }
    }
}
