using System.ComponentModel.DataAnnotations;
using ItemTemplateAttribute = DataAccess.Entities.ItemTemplateAttribute;

namespace DataAccess.Models.Responses
{
    public class ItemTemplateResponse
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; }

        [Required]
        public int EstimatedExpirationDays { get; set; }

        public string? Note { get; set; }

        public string Unit { get; set; }

        public string Image { get; set; }

        public Guid CreatedBy { get; set; }

        public ItemCategoryResponse ItemCategoryResponse { get; set; }

        public List<ItemTemplateAttribute> Attributes { get; set; }

        public List<ItemResponse> ItemTemplateResponses { get; set; }
    }
}
