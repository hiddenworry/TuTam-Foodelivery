using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class ItemTemplateAttribute
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        [ForeignKey(nameof(ItemTemplate))]
        public Guid ItemTemplateId { get; set; }

        public ItemTemplateAttributeStatus Status { get; set; }

        [JsonIgnore]
        public ItemTemplate ItemTemplate { get; set; }

        public List<AttributeValue> AttributeValues { get; set; }
    }
}
