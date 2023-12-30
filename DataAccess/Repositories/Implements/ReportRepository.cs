using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class ReportRepository : IReportRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public ReportRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateReportAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<Report?> FindReportByUserIdAndDeliveryRequestIdAsync(
            Guid userId,
            Guid deliveryRequestId,
            ReportType? reportType
        )
        {
            return await _context.Reports
                .Include(r => r.ScheduledRouteDeliveryRequest)
                .FirstOrDefaultAsync(
                    r =>
                        r.UserId == userId
                        && r.ScheduledRouteDeliveryRequest.DeliveryRequestId == deliveryRequestId
                        && r.Type == reportType
                );
        }

        public async Task<List<Report>?> GetReportsAsync(
            Guid? userId,
            string? keyWord,
            ReportType? reportType
        )
        {
            var query = _context.Reports
                .Include(a => a.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .AsQueryable();

            if (reportType != null)
            {
                query = query.Where(a => a.Type == reportType);
            }
            if (userId != null)
            {
                query = query.Where(a => a.UserId == userId);
            }
            if (!string.IsNullOrEmpty(keyWord))
            {
                keyWord = keyWord.ToLower();

                query = query.Where(
                    a =>
                        (a.User.Name != null && a.User.Name.ToLower().Contains(keyWord))
                        || (a.User.Email != null && a.User.Email.ToLower().Contains(keyWord))
                        || (a.User.Phone != null && a.User.Phone.ToLower().Contains(keyWord))
                );
            }

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }

        public async Task<List<Report>?> GetReportsByDeliveryRequestIdAsync(
            Guid? deliveryRequestId,
            ReportType? reportType
        )
        {
            var query = _context.Reports
                .Include(a => a.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .AsQueryable();

            if (reportType != null)
            {
                query = query.Where(a => a.Type == reportType);
            }
            if (deliveryRequestId != null)
            {
                query = query.Where(
                    a => a.ScheduledRouteDeliveryRequest.DeliveryRequestId == deliveryRequestId
                );
            }

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }

        public async Task<List<Report>?> GetReportsByBranchAsync(
            Guid? branchAdminId,
            ReportType? reportType
        )
        {
            var query = _context.Reports
                .Include(a => a.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.DonatedRequest)
                .ThenInclude(dr => dr!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.Branch)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.DeliveryRequest)
                .ThenInclude(dr => dr.AidRequest)
                .ThenInclude(ar => ar!.CharityUnit)
                .ThenInclude(cu => cu!.User)
                .Include(a => a.ScheduledRouteDeliveryRequest)
                .ThenInclude(a => a.ScheduledRoute)
                .ThenInclude(sr => sr.User)
                .AsQueryable();

            if (branchAdminId != null)
            {
                query = query.Where(
                    a =>
                        a.ScheduledRouteDeliveryRequest.DeliveryRequest.Branch.BranchAdminId
                        == branchAdminId
                );
            }
            if (reportType != null)
            {
                query = query.Where(a => a.Type == reportType);
            }
            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }
    }
}
