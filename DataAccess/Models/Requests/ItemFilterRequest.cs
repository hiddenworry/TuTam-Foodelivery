using DataAccess.EntityEnums;

namespace DataAccess.Models.Requests
{
    public class ItemFilterRequest
    {
        public ItemCategoryType? categoryType { get; set; }

        public Guid? itemCategoryId { get; set; }

        public string? name { get; set; }

        public ItemTemplateStatus? itemStatus { get; set; }
    }
}
