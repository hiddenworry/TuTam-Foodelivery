using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class ItemTemplateAttributeService : IItemTemplateAttributeService
    {
        private readonly IItemTemplateAttributeRepository _itemTemplateAttributeRepository;

        public ItemTemplateAttributeService(
            IItemTemplateAttributeRepository itemTemplateAttributeRepository
        )
        {
            _itemTemplateAttributeRepository = itemTemplateAttributeRepository;
        }
    }
}
