using DataAccess.Entities;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services.Implements
{
    public class ItemCategoryService : IItemCategoryService
    {
        private readonly IItemCategoryRepository _itemCategoryRepository;
        private readonly IConfiguration _config;

        public ItemCategoryService(
            IItemCategoryRepository itemCategoryRepository,
            IConfiguration configuration
        )
        {
            _itemCategoryRepository = itemCategoryRepository;
            _config = configuration;
        }

        public async Task<CommonResponse> CreateItemsCategoryAsync(ItemsCategoryRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string itemCategoryCreateSuccessMsg = _config[
                "ResponseMessages:ItemCategoryMsg:ItemCategoryCreateSuccessMsg"
            ];
            try
            {
                ItemCategory itemCategory = new ItemCategory();
                itemCategory.Name = request.Name;
                itemCategory.Type = request.Type;
                var rs = await _itemCategoryRepository.CreateItemCategoryAsync(itemCategory);
                if (rs != null)
                {
                    commonResponse.Status = 200;
                    commonResponse.Message = itemCategoryCreateSuccessMsg;
                    return commonResponse;
                }
                else
                    throw new Exception();
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return commonResponse;
            }
        }

        public async Task<CommonResponse> GetItemCategoriesListAsync()
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                var rs = await _itemCategoryRepository.GetListItemCategoryAsync();
                commonResponse.Status = 200;
                commonResponse.Data = rs;
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }
    }
}
