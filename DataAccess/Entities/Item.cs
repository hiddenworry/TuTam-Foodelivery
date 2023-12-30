using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Item
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public ItemStatus Status { get; set; }

        [ForeignKey(nameof(ItemTemplate))]
        public Guid ItemTemplateId { get; set; }

        public ItemTemplate ItemTemplate { get; set; }

        public string Image { get; set; }

        [Range(2, 1825)]
        public int EstimatedExpirationDays { get; set; }

        [Range(1, 5000)]
        public double MaximumTransportVolume { get; set; }

        public List<ItemAttributeValue> ItemAttributeValues { get; set; }

        public List<Stock> Stocks { get; set; }

        public List<DonatedItem> DonatedItems { get; set; }

        public List<AidItem> AidItems { get; set; }

        public List<TargetProcess> TargetProcesses { get; set; }
    }
}
