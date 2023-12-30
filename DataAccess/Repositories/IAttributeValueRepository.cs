using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IAttributeValueRepository
    {
        Task<AttributeValue?> CreateAttributeValueAsync(AttributeValue attributeValue);
        Task<AttributeValue?> FindAttributeValueByIdAsync(Guid id);
        Task<AttributeValue?> FindAttributeValueByNameAndAttributeIdAsync(Guid id, string name);
    }
}
