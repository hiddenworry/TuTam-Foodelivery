using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class DeliveryItem
    {
        [Key]
        public Guid Id { get; set; }

        [Range(1, double.MaxValue)]
        public double Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public double? ReceivedQuantity { get; set; }

        [ForeignKey(nameof(DeliveryRequest))]
        public Guid DeliveryRequestId { get; set; }

        public DeliveryRequest DeliveryRequest { get; set; }

        [ForeignKey(nameof(AidItem))]
        public Guid? AidItemId { get; set; }

        public AidItem? AidItem { get; set; }

        [ForeignKey(nameof(DonatedItem))]
        public Guid? DonatedItemId { get; set; }

        public DonatedItem? DonatedItem { get; set; }

        public List<StockUpdatedHistoryDetail> StockUpdatedHistoryDetails { get; set; }
    }
}
