using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ScheduledRouteDeliveryRequest
    {
        [ForeignKey(nameof(ScheduledRoute))]
        public Guid ScheduledRouteId { get; set; }

        public ScheduledRoute ScheduledRoute { get; set; }

        [ForeignKey(nameof(DeliveryRequest))]
        public Guid DeliveryRequestId { get; set; }

        public DeliveryRequest DeliveryRequest { get; set; }

        [ForeignKey(nameof(Report))]
        public Guid? ReportId { get; set; }

        public Report? Report { get; set; }

        public ScheduledRouteDeliveryRequestStatus Status { get; set; }

        [Range(1, int.MaxValue)]
        public int Order { get; set; }

        [Range(0, double.MaxValue)]
        public double TimeToReachThisOrNextAsSeconds { get; set; }

        [Range(0, double.MaxValue)]
        public double DistanceToReachThisOrNextAsMeters { get; set; }
    }
}
