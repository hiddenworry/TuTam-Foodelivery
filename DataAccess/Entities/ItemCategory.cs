using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class ItemCategory
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        [Required]
        public ItemCategoryType Type { get; set; }

        public List<ItemTemplate> ItemTemplates { get; set; }
    }
}
