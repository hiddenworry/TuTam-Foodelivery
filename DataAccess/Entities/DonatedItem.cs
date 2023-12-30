using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class DonatedItem
    {
        [Key]
        public Guid Id { get; set; }

        [Range(1, double.MaxValue)]
        public double Quantity { get; set; }

        public DateTime InitialExpirationDate { get; set; }

        public DonatedItemStatus Status { get; set; }

        [ForeignKey(nameof(DonatedRequest))]
        public Guid DonatedRequestId { get; set; }

        public DonatedRequest DonatedRequest { get; set; }

        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        public Item Item { get; set; }

        public List<DeliveryItem> DeliveryItems { get; set; }
    }
}
