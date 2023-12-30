using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class DonatedItemService : IDonatedItemService
    {
        private readonly IDonatedItemRepository _donatedItemRepository;
        private readonly ILogger<DonatedItemService> _logger;
        private readonly IConfiguration _config;

        public DonatedItemService(
            IDonatedItemRepository donatedItemRepository,
            ILogger<DonatedItemService> logger,
            IConfiguration configuration
        )
        {
            _donatedItemRepository = donatedItemRepository;
            _config = configuration;
            _logger = logger;
        }

        public async Task<CommonResponse> GetDonatedItemAsync(
            int? page,
            int? pageSize,
            Guid userId,
            string? keyWord,
            DonatedItemStatus? status,
            DateTime? startDate,
            DateTime? endDate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<DonatedItem>? donatedItems =
                    await _donatedItemRepository.GetHistoryDonatedItemOfUserAsync(
                        userId,
                        startDate,
                        endDate,
                        keyWord,
                        status
                    );
                if (donatedItems != null && donatedItems.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = donatedItems.Count;

                    donatedItems = donatedItems
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var rs = donatedItems
                        .Select(
                            a =>
                                new DonatedItemResponse
                                {
                                    Id = a.Id,
                                    InitialExpirationDate = a.InitialExpirationDate,
                                    Quantity = a.Quantity,
                                    Status = a.Status.ToString(),
                                    ItemTemplateResponse = new ItemResponse
                                    {
                                        Id = a.ItemId,
                                        EstimatedExpirationDays = a.Item.EstimatedExpirationDays,
                                        Name = a.Item.ItemTemplate.Name,
                                        Image = a.Item.Image,
                                        AttributeValues = a.Item.ItemAttributeValues
                                            .Select(itav => itav.AttributeValue.Value)
                                            .ToList(),
                                        MaximumTransportVolume = a.Item.MaximumTransportVolume,
                                        Note = a.Item.Note ?? string.Empty,
                                        Unit = a.Item.ItemTemplate.Unit.ToString() ?? string.Empty
                                    }
                                }
                        )
                        .ToList();
                    commonResponse.Data = rs;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    commonResponse.Data = new List<string>();
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(DonatedItemService);
                string methodName = nameof(GetDonatedItemAsync);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }
    }
}
