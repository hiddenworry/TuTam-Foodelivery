using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IItemAttributeValueRepository
    {
        Task<ItemAttributeValue?> CreateItemAttributeValueAsync(
            ItemAttributeValue itemTemplateAttributeValue
        );
    }
}
