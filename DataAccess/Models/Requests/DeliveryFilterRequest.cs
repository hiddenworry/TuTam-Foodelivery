using DataAccess.EntityEnums;
using DataAccess.ModelsEnum;

namespace DataAccess.Models.Requests
{
    public class DeliveryFilterRequest
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? KeyWord { get; set; }

        public string? Address { get; set; }

        public Guid? ItemId { get; set; }

        public DeliveryType? DeliveryType { get; set; }

        public DeliveryRequestStatus? Status { get; set; }

        public Guid? CharityUnitId { get; set; }

        public Guid? BranchId { get; set; }

        public Guid? BranchAdminId { get; set; }
    }
}
