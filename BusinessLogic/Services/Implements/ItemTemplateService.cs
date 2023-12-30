using BusinessLogic.Utils.FirebaseService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Transactions;
using ItemTemplateAttribute = DataAccess.Entities.ItemTemplateAttribute;

namespace BusinessLogic.Services.Implements
{
    public class ItemTemplateService : IItemTemplateService
    {
        private readonly IItemTemplateRepository _itemTemplateRepository;
        private readonly IItemTemplateAttributeRepository _attributeRepository;
        private readonly IItemCategoryRepository _itemCategoryRepository;
        private readonly IAttributeValueRepository _attributeValueRepository;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IConfiguration _config;
        private readonly IItemRepository _itemRepositoryReal;
        private readonly ILogger<ItemTemplateService> _logger;
        private readonly IUserRepository _userRepository;

        public ItemTemplateService(
            IItemTemplateRepository itemTemplateRepository,
            IFirebaseStorageService firebaseStorageService,
            IItemTemplateAttributeRepository attributeRepository,
            IAttributeValueRepository attributeValueRepository,
            IConfiguration configuration,
            IItemCategoryRepository itemCategoryRepository,
            IItemRepository itemRepositoryReal,
            ILogger<ItemTemplateService> logger,
            IUserRepository userRepository
        )
        {
            _itemTemplateRepository = itemTemplateRepository;
            _firebaseStorageService = firebaseStorageService;
            _attributeRepository = attributeRepository;
            _attributeValueRepository = attributeValueRepository;
            _config = configuration;
            _itemCategoryRepository = itemCategoryRepository;
            _itemRepositoryReal = itemRepositoryReal;
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse> CreateItemTemplate(
            ItemTemplateRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string itemCreateSuccessMsg = _config["ResponseMessages:ItemMsg:ItemCreateSuccessMsg"];
            string itemCategoryNotFoundMsg = _config[
                "ResponseMessages:ItemMsg:ItemCategoryNotFoundMsg"
            ];
            string attributeUpdateErrorMsg = _config[
                "ResponseMessages:ItemMsg:AttributeUpdateErrorMsg"
            ];
            try
            {
                var itemCategory = await _itemCategoryRepository.FindItemCategoryByIdAsync(
                    request.ItemcategoryId
                );
                if (itemCategory == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = itemCategoryNotFoundMsg;
                    return commonResponse;
                }
                if (await _itemTemplateRepository.CheckDuplicatedName(request.Name) != null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Tên vật phẩm này đã tồn tại";
                    return commonResponse;
                }
                if (
                    itemCategory != null
                    && itemCategory.Type == ItemCategoryType.FOOD
                    && request.ItemTemplates != null
                    && request.ItemTemplates.Any(a => a.EstimatedExpirationDays <= 0)
                )
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Thực phẩm bắc buộc phải có hạn sử dụng cố định.";
                    return commonResponse;
                }

                ItemTemplate itemTemplate = CreateItemTemplateFromRequest(request, userId);
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var rs = await _itemTemplateRepository.CreateItemTemplateAsync(itemTemplate);
                    if (request.Attributes != null && rs != null)
                    {
                        var attr = await CreateAttributesForItem(request.Attributes, rs.Id, userId);
                        if (request.ItemTemplates != null)
                        {
                            await CreateItems(request.ItemTemplates, rs.Id, attr!);
                        }
                    }
                    else if (request.Attributes == null && rs != null)
                    {
                        Item item = new Item();
                        item.Status = ItemStatus.ACTIVE;
                        item.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                        item.ItemTemplateId = itemTemplate.Id;
                        await _itemRepositoryReal.CreateItemAsync(item);
                    }
                    else
                        throw new Exception();
                    scope.Complete();
                }
                commonResponse.Status = 200;
                commonResponse.Message = itemCreateSuccessMsg;
                return commonResponse;
            }
            catch (ArgumentException)
            {
                commonResponse.Status = 400;
                commonResponse.Message = attributeUpdateErrorMsg;
                return commonResponse;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(CreateItemTemplate);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return commonResponse;
            }
        }

        private ItemTemplate CreateItemTemplateFromRequest(ItemTemplateRequest request, Guid userId)
        {
            return new ItemTemplate
            {
                Name = request.Name.Trim(),
                ItemUnitId = request.ItemUnitId,
                Note = request.Note,
                Status = request.Status,
                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                ItemcategoryId = request.ItemcategoryId,
                Image = request.ImageUrl
            };
        }

        private async Task<List<ItemTemplateAttribute>?> CreateAttributesForItem(
            List<AttributeRequest> attributes,
            Guid itemId,
            Guid userId
        )
        {
            try
            {
                List<ItemTemplateAttribute> rs = new List<ItemTemplateAttribute>();
                foreach (var attributeRequest in attributes)
                {
                    ItemTemplateAttribute attribute = new ItemTemplateAttribute
                    {
                        Name = attributeRequest.Name.Trim(),
                        ItemTemplateId = itemId,
                        Status = attributeRequest.Status
                    };

                    List<AttributeValue> attributeValues = attributeRequest.AttributeValues
                        .Select(
                            av =>
                                new AttributeValue
                                {
                                    Value = av.name.Trim(),
                                    ItemTemplateAttributeId = attribute.Id
                                }
                        )
                        .ToList();

                    if (attributeValues.Count > 0)
                    {
                        attribute.AttributeValues = attributeValues;
                    }

                    ItemTemplateAttribute? attr =
                        await _attributeRepository.CreateItemTemplateAttributeAsync(attribute);
                    if (attr != null)
                    {
                        rs.Add(attr);
                    }
                }

                return rs;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(CreateAttributesForItem);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                return null;
            }
        }

        private async Task CreateItems(
            List<ItemRequest> templates,
            Guid itemId,
            List<ItemTemplateAttribute> attributes
        )
        {
            foreach (var template in templates)
            {
                Item item = new Item
                {
                    Status = ItemStatus.ACTIVE,
                    Note = template.Note,
                    ItemTemplateId = itemId,
                    CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                    Image = template.ImageUrl,
                    EstimatedExpirationDays = template.EstimatedExpirationDays,
                    MaximumTransportVolume = template.maximumTransportVolume
                };

                List<ItemAttributeValue> itemTemplateAttributeValues =
                    new List<ItemAttributeValue>();

                List<Guid> checkDuplicatedAttribute = new List<Guid>();

                foreach (var value in template.Values)
                {
                    AttributeValue? attributeValue = attributes
                        .FirstOrDefault(a => a.AttributeValues.Any(av => av.Value == value))
                        ?.AttributeValues.FirstOrDefault(av => av.Value == value);

                    if (attributeValue != null)
                    {
                        itemTemplateAttributeValues.Add(
                            new ItemAttributeValue
                            {
                                AttributeValueId = attributeValue.Id,
                                Item = item
                            }
                        );
                        if (
                            !checkDuplicatedAttribute.Contains(
                                attributeValue.ItemTemplateAttributeId
                            )
                        )
                        {
                            checkDuplicatedAttribute.Add(attributeValue.ItemTemplateAttributeId);
                        }
                        else
                            throw new ArgumentException();
                    }
                }
                item.ItemAttributeValues = itemTemplateAttributeValues;
                await _itemRepositoryReal.CreateItemAsync(item);
            }
        }

        public async Task<CommonResponse> UpdateItemTemplate(
            ItemTemplateRequest request,
            Guid itemId,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string itemUpdateSuccessMsg = _config["ResponseMessages:ItemMsg:ItemUpdateSuccessMsg"];
            string itemCategoryNotFoundMsg = _config[
                "ResponseMessages:ItemMsg:ItemCategoryNotFoundMsg"
            ];
            string itemNotFoundMsg = _config["ResponseMessages:ItemMsg:ItemNotFoundMsg"];

            try
            {
                ItemCategory? itemCategory =
                    await _itemCategoryRepository.FindItemCategoryByIdAsync(request.ItemcategoryId);
                if (itemCategory == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = itemCategoryNotFoundMsg;
                    return commonResponse;
                }

                var itemTemplate = await _itemTemplateRepository.FindItemTemplateByIdAsync(itemId);
                if (itemTemplate != null)
                {
                    ItemTemplate? checkDuplicatedName =
                        await _itemTemplateRepository.CheckDuplicatedName(request.Name);
                    if (checkDuplicatedName != null && checkDuplicatedName.Id != itemTemplate.Id)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Tên vật phẩm này đã tồn tại";
                        return commonResponse;
                    }
                    if (
                        itemCategory != null
                        && itemCategory.Type == ItemCategoryType.FOOD
                        && request.ItemTemplates != null
                        && request.ItemTemplates.Any(a => a.EstimatedExpirationDays <= 0)
                    )
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Thực phẩm bắc buộc phải có hạn sử dụng cố định.";
                        return commonResponse;
                    }

                    itemTemplate.Name = request.Name.Trim();
                    itemTemplate.ItemUnitId = request.ItemUnitId;
                    itemTemplate.Note = request.Note;
                    itemTemplate.Status = request.Status;
                    itemTemplate.ItemcategoryId = request.ItemcategoryId;
                    itemTemplate.Image = request.ImageUrl;

                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        var rs = await _itemTemplateRepository.UpdateItemTemplateAsync(
                            itemTemplate
                        );
                        if (request.Attributes != null && rs != null)
                        {
                            await UpdateAttributesForItem(request.Attributes, itemId, userId);
                        }
                        if (request.ItemTemplates != null && request.Attributes != null)
                        {
                            await UpdateItemTemplates(request.ItemTemplates, rs!.Id);
                        }
                        if (
                            request.ItemTemplates != null
                            && (request.Attributes == null || request.Attributes.Count == 0)
                        )
                        {
                            foreach (var it in request.ItemTemplates)
                            {
                                Item? item = await _itemRepositoryReal.FindItemByIdAsync(it.Id);
                                if (item != null)
                                {
                                    item.Note = it.Note;
                                    item.EstimatedExpirationDays = it.EstimatedExpirationDays;
                                    item.MaximumTransportVolume = it.maximumTransportVolume;
                                    item.Status = it.Status;
                                    await _itemRepositoryReal.UpdateItemAsync(item);
                                }
                            }
                        }
                        scope.Complete();
                        commonResponse.Status = 200;
                        commonResponse.Message = itemUpdateSuccessMsg;
                        return commonResponse;
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = itemNotFoundMsg;
                    return commonResponse;
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return commonResponse;
            }
        }

        private async Task<List<ItemTemplateAttribute>?> UpdateAttributesForItem(
            List<AttributeRequest> attributes,
            Guid itemId,
            Guid userId
        )
        {
            var updatedAttributes = new List<ItemTemplateAttribute>();

            foreach (var attributeRequest in attributes)
            {
                if (attributeRequest == null)
                    continue;

                ItemTemplateAttribute? attribute = null;

                if (attributeRequest?.Id == null || attributeRequest.Id == Guid.Empty)
                {
                    attribute = new ItemTemplateAttribute
                    {
                        Name = attributeRequest!.Name,
                        ItemTemplateId = itemId,
                        Status = attributeRequest.Status
                    };
                    attribute.AttributeValues = attributeRequest.AttributeValues
                        .Where(av => !string.IsNullOrWhiteSpace(av.name))
                        .Select(av => new AttributeValue { Value = av.name.Trim() })
                        .ToList();
                }
                else
                {
                    attribute = await _attributeRepository.FindItemTemplateAttributeByIdAsync(
                        attributeRequest.Id
                    );
                    if (attribute != null)
                    {
                        attribute.Name = attributeRequest.Name.Trim();
                        attribute.Status = attributeRequest.Status;
                        var updatedAttributeValues = new List<AttributeValue>();

                        foreach (var av in attributeRequest.AttributeValues)
                        {
                            if (av?.Id == null || av.Id == Guid.Empty)
                            {
                                updatedAttributeValues.Add(
                                    new AttributeValue
                                    {
                                        Value = av!.name,
                                        ItemTemplateAttributeId = attribute.Id
                                    }
                                );
                            }
                            else
                            {
                                var existingValue = attribute.AttributeValues.FirstOrDefault(
                                    avv => avv.Id == av.Id
                                );

                                if (existingValue != null)
                                {
                                    existingValue.Value = av.name.Trim();
                                }
                            }
                        }

                        attribute.AttributeValues.AddRange(updatedAttributeValues);
                    }
                }

                if (attribute != null)
                {
                    ItemTemplateAttribute? updatedAttribute =
                        await _attributeRepository.UpdateItemTemplateAttributeAsync(attribute);
                    if (updatedAttribute != null)
                    {
                        updatedAttributes.Add(updatedAttribute);
                    }
                }
            }

            return updatedAttributes;
        }

        private async Task UpdateItemTemplates(List<ItemRequest> templates, Guid itemId)
        {
            try
            {
                List<ItemTemplateAttribute>? attributes =
                    await _attributeRepository?.FindItemTemplateAttributeByItemTemplateIdAsync(
                        itemId
                    )!;
                if (attributes != null && attributes.Count > 0)
                {
                    foreach (var template in templates)
                    {
                        var tmp = await _itemRepositoryReal.FindItemByIdAsync(template.Id);
                        var itemTemplate =
                            tmp
                            ?? new Item
                            {
                                Status = ItemStatus.ACTIVE,
                                Note = template.Note,
                                ItemTemplateId = itemId,
                                CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                                Image = template.ImageUrl,
                                EstimatedExpirationDays = template.EstimatedExpirationDays,
                                MaximumTransportVolume = template.maximumTransportVolume
                            };

                        var itemTemplateAttributeValues = template.Values
                            .Where(value => !string.IsNullOrWhiteSpace(value.Trim()))
                            .Select(value =>
                            {
                                var attributeValue = attributes
                                    .SelectMany(a => a.AttributeValues)
                                    .FirstOrDefault(av => av.Value == value.Trim());

                                return new ItemAttributeValue
                                {
                                    AttributeValueId = attributeValue?.Id ?? Guid.Empty,
                                    Item = itemTemplate
                                };
                            })
                            .ToList();

                        itemTemplate.ItemAttributeValues = itemTemplateAttributeValues;

                        if (tmp == null)
                        {
                            await _itemRepositoryReal.CreateItemAsync(itemTemplate);
                        }
                        else
                        {
                            itemTemplate.Status = template.Status;
                            itemTemplate.Image = template.ImageUrl;
                            itemTemplate.Note = template.Note;
                            itemTemplate.EstimatedExpirationDays = template.EstimatedExpirationDays;
                            itemTemplate.MaximumTransportVolume = template.maximumTransportVolume;
                            await _itemRepositoryReal.UpdateItemAsync(itemTemplate);
                        }
                    }
                }
            }
            catch { }
        }

        public async Task<CommonResponse> GetItemTemplatesAsync(
            Guid userId,
            ItemFilterRequest itemFilterRequest,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        )
        {
            CommonResponse commonResponse = new CommonResponse();

            try
            {
                var filteredItems = await _itemTemplateRepository.FindItemTemplatesAsync(
                    itemFilterRequest
                );
                User? user = await _userRepository.FindUserByIdAsync(userId);
                if (filteredItems != null && filteredItems.Count > 0 && user != null)
                {
                    if (
                        user.Role.Name != RoleEnum.SYSTEM_ADMIN.ToString()
                        || user.Role.Name == RoleEnum.BRANCH_ADMIN.ToString()
                    )
                    {
                        filteredItems = filteredItems
                            .Where(i => i.Status == ItemTemplateStatus.ACTIVE)
                            .ToList();
                    }

                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = filteredItems.Count;
                    filteredItems = filteredItems
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();

                    var itemResponses = filteredItems
                        .Select(
                            item =>
                                new
                                {
                                    item.Id,
                                    item.Name,
                                    item.CreatedDate,
                                    Status = item.Status.ToString(),
                                    item.Note,
                                    item.Unit,
                                    item.Image,
                                    ItemCategoryResponse = new ItemCategoryResponse
                                    {
                                        Id = item.ItemcategoryId,
                                        Name = item.ItemCategory.Name,
                                        Type = item.ItemCategory.Type.ToString()
                                    },
                                    Attributes = item.ItemTemplateAttributes.Select(
                                        attr =>
                                            new
                                            {
                                                attr.Name,
                                                attr.Id,
                                                Status = attr.Status.ToString()
                                            }
                                    )
                                }
                        )
                        .ToList();

                    if (sortType == SortType.ASC)
                    {
                        itemResponses = itemResponses.OrderBy(u => u.Name).ToList();
                    }
                    else
                    {
                        itemResponses = itemResponses.OrderByDescending(u => u.Name).ToList();
                    }
                    commonResponse.Pagination = pagination;
                    commonResponse.Data = itemResponses;
                }

                commonResponse.Message = "Success";
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(GetItemTemplatesAsync);
                string methodName = nameof(ItemTemplateService);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = "An error occurred.";
                commonResponse.Status = 500;
            }

            return commonResponse;
        }

        public async Task<CommonResponse> GetItemTemplateById(Guid id, Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                if (
                    user != null
                    && user.Role != null
                    && (
                        user.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString()
                        || user.Role.Name == RoleEnum.BRANCH_ADMIN.ToString()
                    )
                )
                {
                    commonResponse = await GetItemTemplateByIdForAdmin(id);
                }
                else
                {
                    commonResponse = await GetItemTemplateByIdForUser(id);
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(GetItemTemplateByIdForAdmin);
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

        private async Task<CommonResponse> GetItemTemplateByIdForAdmin(Guid id)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                ItemTemplate? item = await _itemTemplateRepository.GetItemTemplateByIdAsync(id);

                if (item != null)
                {
                    var itemResponses = new
                    {
                        item.Id,
                        item.Name,
                        item.CreatedDate,
                        Status = item.Status.ToString(),
                        item.Note,
                        item.Unit,
                        item.Image,
                        ItemCategoryResponse = new ItemCategoryResponse
                        {
                            Id = item.ItemcategoryId,
                            Name = item.ItemCategory.Name,
                            Type = item.ItemCategory.Type.ToString()
                        },
                        Attributes = item.ItemTemplateAttributes.Select(
                            attr =>
                                new
                                {
                                    attr.Name,
                                    attr.Id,
                                    Status = attr.Status.ToString(),
                                    attr.AttributeValues
                                }
                        ),
                        ItemTemplateResponses = item.Items
                            .Select(
                                itemTemplate =>
                                    new
                                    {
                                        itemTemplate.Note,
                                        item.Name,
                                        itemTemplate.Id,
                                        itemTemplate.Image,
                                        itemTemplate.EstimatedExpirationDays,
                                        Status = itemTemplate.Status.ToString(),
                                        itemTemplate.MaximumTransportVolume,
                                        Attributes = itemTemplate.ItemAttributeValues
                                            .Select(
                                                a =>
                                                    new
                                                    {
                                                        a.AttributeValue.ItemTemplateAttributeId,
                                                        a.AttributeValue.ItemTemplateAttribute.Name,
                                                        Status = a.AttributeValue.ItemTemplateAttribute.Status.ToString(),
                                                        AttributeValue = new
                                                        {
                                                            a.AttributeValue.Value,
                                                            a.AttributeValueId
                                                        }
                                                    }
                                            )
                                            .ToList()
                                    }
                            )
                            .ToList()
                    };
                    commonResponse.Data = itemResponses;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(GetItemTemplateByIdForAdmin);
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

        public async Task<CommonResponse> GetItemTemplateByIdForUser(Guid id)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                ItemTemplate? item = await _itemTemplateRepository.GetItemTemplateByIdAsync(id);

                if (item != null && item.Status != ItemTemplateStatus.INACTIVE)
                {
                    var itemResponses = new
                    {
                        item.Id,
                        item.Name,
                        item.CreatedDate,
                        Status = item.Status.ToString(),
                        item.Note,
                        item.Unit,
                        item.Image,
                        ItemCategoryResponse = new ItemCategoryResponse
                        {
                            Id = item.ItemcategoryId,
                            Name = item.ItemCategory.Name,
                            Type = item.ItemCategory.Type.ToString()
                        },
                        Attributes = item.ItemTemplateAttributes.Select(
                            attr =>
                                new
                                {
                                    attr.Name,
                                    attr.Id,
                                    Status = attr.Status.ToString(),
                                    attr.AttributeValues
                                }
                        ),
                        ItemTemplateResponses = item.Items
                            .Select(
                                itemTemplate =>
                                    new
                                    {
                                        itemTemplate.Note,
                                        item.Name,
                                        itemTemplate.Id,
                                        itemTemplate.Image,
                                        itemTemplate.EstimatedExpirationDays,
                                        Status = itemTemplate.Status.ToString(),
                                        itemTemplate.MaximumTransportVolume,
                                        Attributes = itemTemplate.ItemAttributeValues
                                            .Select(
                                                a =>
                                                    new
                                                    {
                                                        a.AttributeValue.ItemTemplateAttributeId,
                                                        a.AttributeValue.ItemTemplateAttribute.Name,
                                                        Status = a.AttributeValue.ItemTemplateAttribute.Status.ToString(),
                                                        AttributeValue = new
                                                        {
                                                            a.AttributeValue.Value,
                                                            a.AttributeValueId
                                                        }
                                                    }
                                            )
                                            .Where(
                                                ite => ite.Status != ItemStatus.INACTIVE.ToString()
                                            )
                                            .ToList()
                                    }
                            )
                            .ToList()
                    };
                    commonResponse.Data = itemResponses;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(GetItemTemplateByIdForAdmin);
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

        public async Task<CommonResponse> SearchItemTemplateByKeyWord(
            string searchKeyWord,
            int? pageSize,
            int? page
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<ItemTemplate?>? rs = await _itemTemplateRepository.SearchItemTemplate(
                    searchKeyWord,
                    null
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

                    var itemResponses = rs.Select(
                            item =>
                                new ItemTemplateResponse
                                {
                                    Id = item!.Id,
                                    Name = item.Name,
                                    CreatedDate = item.CreatedDate,
                                    Status = item.Status.ToString(),
                                    Note = item.Note,
                                    Unit = item.Unit.Name,
                                    Image = item.Image,
                                    ItemCategoryResponse = new ItemCategoryResponse
                                    {
                                        Id = item.ItemcategoryId,
                                        Name = item.ItemCategory.Name,
                                        Type = item.ItemCategory.Type.ToString()
                                    },
                                    Attributes = item.ItemTemplateAttributes,
                                    ItemTemplateResponses = item.Items
                                        .Select(
                                            itemTemplate =>
                                                new ItemResponse
                                                {
                                                    Note = itemTemplate.Note,
                                                    Name = item.Name,
                                                    Id = itemTemplate.Id,
                                                    AttributeValues =
                                                        itemTemplate.ItemAttributeValues
                                                            .Select(av => av.AttributeValue.Value)
                                                            .ToList()
                                                }
                                        )
                                        .ToList()
                                }
                        )
                        .ToList();

                    commonResponse.Pagination = pagination;
                    commonResponse.Data = itemResponses;
                    commonResponse.Message = "Success";
                    commonResponse.Status = 200;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(SearchItemTemplateByKeyWord);
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

        public async Task<CommonResponse> DeleteItemById(Guid id)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            string itemMsg = _config["ResponseMessages:ItemMsg:ItemNotFoundMsg"];
            try
            {
                ItemTemplate? rs = await _itemTemplateRepository.FindItemTemplateByIdAsync(id);
                if (rs != null)
                {
                    rs.Status = ItemTemplateStatus.INACTIVE;
                    await _itemTemplateRepository.UpdateItemTemplateAsync(rs);
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = itemMsg;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(ItemTemplateService);
                string methodName = nameof(DeleteItemById);
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
