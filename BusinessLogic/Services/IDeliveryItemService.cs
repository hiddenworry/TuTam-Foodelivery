using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IDeliveryItemService
    {
        Task<CommonResponse> GetDeliveredItemByCharityUnit(Guid userId, int? page, int? pageSize);
    }
}
