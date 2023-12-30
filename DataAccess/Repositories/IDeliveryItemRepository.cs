using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IDeliveryItemRepository
    {
        Task<int> AddDeliveryItemsAsync(List<DeliveryItem> deliveryItems);
        Task<List<DeliveryItem>?> GetByDeliveredItemByCharityUnitId(Guid charityId);
        Task<List<DeliveryItem>> GetDeliveryItemsByDeliveryRequestIdAsync(Guid deliveryRequestId);

        Task<List<DeliveryItem>> GetPendingDeliveryItemsByItemIdAndBranchIdOfAidDeliveryRequest(
            Guid itemId,
            Guid branchId
        );
        Task<int> UpdateDeliveryItemAsync(DeliveryItem deliveryItem);
        Task<int> UpdateDeliveryItemsAsync(List<DeliveryItem> deliveryItems);
    }
}
