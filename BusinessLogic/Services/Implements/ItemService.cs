using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemTemplateRepository _itemTemplateRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<IItemTemplateService> _logger;

        public ItemService(
            IItemTemplateRepository itemTemplateRepository,
            IItemRepository itemRepository,
            IConfiguration configuration,
            ILogger<IItemTemplateService> logger
        )
        {
            _itemTemplateRepository = itemTemplateRepository;
            _itemRepository = itemRepository;
            _config = configuration;
            _logger = logger;
        }

        public async Task<CommonResponse> SearchItemForUser(
            string? searchStr,
            ItemCategoryType? itemCategory,
            Guid? itemCategoryId,
            int? page,
            int? pageSize
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<Item>? rs = await _itemRepository.SelectRelevanceByKeyWordAsync(
                    searchStr,
                    pageSize,
                    itemCategory,
                    itemCategoryId
                );

                if (rs != null)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = rs.Count;
                    rs = rs.Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var res = rs.Select(
                            it =>
                                new
                                {
                                    itemTemplateId = it.Id,
                                    name = it.ItemTemplate.Name,
                                    image = it.Image,
                                    unit = it.ItemTemplate.Unit,
                                    it.Note,
                                    Attributes = it.ItemAttributeValues.Select(
                                        ita => new { attributeValue = ita.AttributeValue.Value, }
                                    )
                                }
                        )
                        .Distinct();

                    commonResponse.Data = res;
                    commonResponse.Pagination = pagination;
                    // commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemService);
                string methodName = nameof(SearchItemForUser);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetItemById(Guid itemId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Item? rs = await _itemRepository.FindItemByIdAsync(itemId);

                if (rs != null)
                {
                    ItemResponse itemTemplateResponse = new ItemResponse();
                    itemTemplateResponse.AttributeValues = rs.ItemAttributeValues
                        .Select(ita => ita.AttributeValue.Value)
                        .ToList();
                    itemTemplateResponse.Name = rs.ItemTemplate.Name;
                    itemTemplateResponse.Id = rs.Id;
                    itemTemplateResponse.MaximumTransportVolume = rs.MaximumTransportVolume;
                    itemTemplateResponse.EstimatedExpirationDays = rs.EstimatedExpirationDays;
                    itemTemplateResponse.Note = rs.Note ?? string.Empty;
                    itemTemplateResponse.Unit = rs.ItemTemplate.Unit.Name.ToString();
                    itemTemplateResponse.Image = rs.Image;
                    itemTemplateResponse.CategoryResponse = new ItemCategoryResponse
                    {
                        Id = rs.ItemTemplate.ItemcategoryId,
                        Name = rs.ItemTemplate.ItemCategory.Name,
                        Type = rs.ItemTemplate.ItemCategory.Type.ToString()
                    };
                    commonResponse.Data = itemTemplateResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemService);
                string methodName = nameof(GetItemById);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }
    }
}
