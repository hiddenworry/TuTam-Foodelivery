using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class ItemAttributeValueService : IItemAttributeValueService
    {
        private readonly IItemAttributeValueRepository _itemAttributeValueRepository;

        public ItemAttributeValueService(IItemAttributeValueRepository itemAttributeValueRepository)
        {
            _itemAttributeValueRepository = itemAttributeValueRepository;
        }
    }
}
