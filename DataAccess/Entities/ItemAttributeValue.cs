using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ItemAttributeValue
    {
        [ForeignKey(nameof(Item))]
        public Guid ItemId { get; set; }

        [ForeignKey(nameof(AttributeValue))]
        public Guid AttributeValueId { get; set; }

        public Item Item { get; set; }

        public AttributeValue AttributeValue { get; set; }
    }
}
