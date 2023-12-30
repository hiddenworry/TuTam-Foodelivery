using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class AttributeRequest
    {
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [Required]
        public string Name { get; set; }

        [Required]
        public ItemTemplateAttributeStatus Status { get; set; }

        public List<AttributeValueRequest> AttributeValues { get; set; }
    }
}
