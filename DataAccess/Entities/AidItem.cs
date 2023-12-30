using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DataAccess.EntityEnums;

namespace DataAccess.Entities
{
    public class AidItem
    {
        [Key]
        public Guid Id { get; set; }

        [Range(1, double.MaxValue)]
        public double Quantity { get; set; }

        public AidItemStatus Status { get; set; }

        [ForeignKey(nameof(AidRequest))]
        public Guid AidRequestId { get; set; }

        public AidRequest AidRequest { get; set; }

        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        public Item Item { get; set; }

        public List<DeliveryItem> DeliveryItems { get; set; }
    }
}
