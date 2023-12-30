using DataAccess.Entities;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class DeliveryItemService : IDeliveryItemService
    {
        private readonly IDeliveryItemRepository _deliveryItemRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<DeliveryItemService> _logger;
        private readonly ICharityUnitRepository _charityUnitRepository;

        public DeliveryItemService(
            IDeliveryItemRepository deliveryItemRepository,
            IConfiguration configuration,
            ILogger<DeliveryItemService> logger,
            ICharityUnitRepository charityUnitRepository
        )
        {
            _deliveryItemRepository = deliveryItemRepository;
            _config = configuration;
            _logger = logger;
            _charityUnitRepository = charityUnitRepository;
        }

        public async Task<CommonResponse> GetDeliveredItemByCharityUnit(
            Guid userId,
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
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindActiveCharityUnitsByUserIdAsync(userId);
                Guid tmpCharityUnitId = Guid.Empty;
                if (charityUnit != null)
                {
                    tmpCharityUnitId = charityUnit.Id;
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy tổ chức từ thiện.";
                    return commonResponse;
                }

                List<DeliveryItem>? deliveryItems =
                    await _deliveryItemRepository.GetByDeliveredItemByCharityUnitId(
                        tmpCharityUnitId
                    );
                if (deliveryItems != null && deliveryItems.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = deliveryItems.Count;

                    var rs = deliveryItems
                        .Where(a => a.AidItem != null)
                        .Select(
                            a =>
                                new
                                {
                                    a.AidItem!.Item.ItemTemplate.Name,
                                    a.AidItem.Item.Image,
                                    a.Quantity,
                                    CreateDate = a.DeliveryRequest.CreatedDate,
                                    Unit = a.AidItem.Item.ItemTemplate.Unit.Name,
                                    AttributeValue = a.AidItem.Item.ItemAttributeValues
                                        .Select(itav => itav.AttributeValue.Value)
                                        .ToList(),
                                    FromBranch = a.DeliveryRequest.Branch.Name
                                }
                        )
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                string className = nameof(DeliveryItemService);
                string methodName = nameof(GetDeliveredItemByCharityUnit);
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
