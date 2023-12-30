using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class AttributeValue
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Value { get; set; }

        [ForeignKey(nameof(ItemTemplateAttribute))]
        public Guid ItemTemplateAttributeId { get; set; }

        [JsonIgnore]
        public ItemTemplateAttribute ItemTemplateAttribute { get; set; }

        [JsonIgnore]
        public List<ItemAttributeValue> ItemAttributeValues { get; set; }
    }
}
