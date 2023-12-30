using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class ItemTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public ItemTemplateStatus Status { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public string Image { get; set; }

        [ForeignKey(nameof(ItemCategory))]
        public Guid ItemcategoryId { get; set; }

        [ForeignKey(nameof(Unit))]
        public Guid ItemUnitId { get; set; }

        public ItemUnit Unit { get; set; }

        public ItemCategory ItemCategory { get; set; }

        public List<ItemTemplateAttribute> ItemTemplateAttributes { get; set; }

        public List<Item> Items { get; set; }
    }
}
