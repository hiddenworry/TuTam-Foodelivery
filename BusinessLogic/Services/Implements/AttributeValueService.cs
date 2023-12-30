using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class AttributeValueService : IAttributeValueService
    {
        private readonly IAttributeValueRepository _attributeValueRepository;

        public AttributeValueService(IAttributeValueRepository attributeValueRepository)
        {
            _attributeValueRepository = attributeValueRepository;
        }
    }
}
