using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class StockUpdatedHistoryDetail
    {
        [Key]
        public Guid Id { get; set; }

        [Range(0, double.MaxValue)]
        public double Quantity { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [ForeignKey(nameof(StockUpdatedHistory))]
        public Guid StockUpdatedHistoryId { get; set; }

        public StockUpdatedHistory StockUpdatedHistory { get; set; }

        [ForeignKey(nameof(DeliveryItem))]
        public Guid? DeliveryItemId { get; set; }

        public DeliveryItem? DeliveryItem { get; set; }

        [ForeignKey(nameof(Stock))]
        public Guid? StockId { get; set; }

        public Stock? Stock { get; set; }

        [ForeignKey(nameof(AidRequest))]
        public Guid? AidRequestId { get; set; }

        public AidRequest? AidRequest { get; set; }
    }
}
