using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IItemTemplateAttributeRepository
    {
        Task<ItemTemplateAttribute?> CreateItemTemplateAttributeAsync(
            ItemTemplateAttribute attribute
        );
        Task<ItemTemplateAttribute?> FindItemTemplateAttributeByIdAsync(Guid Id);

        Task<ItemTemplateAttribute?> FindItemTemplateAttributeByNameAndItemTempateIdAsync(
            string name,
            Guid itemId
        );
        Task<ItemTemplateAttribute?> UpdateItemTemplateAttributeAsync(
            ItemTemplateAttribute attribute
        );
        Task<List<ItemTemplateAttribute>>? FindItemTemplateAttributeByItemTemplateIdAsync(
            Guid itemTemplateId
        );
    }
}
