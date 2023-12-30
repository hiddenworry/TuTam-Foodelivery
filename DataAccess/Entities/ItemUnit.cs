using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class ItemUnit
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Symbol { get; set; }

        [JsonIgnore]
        public List<ItemTemplate> ItemTemplates { get; set; }
    }
}
