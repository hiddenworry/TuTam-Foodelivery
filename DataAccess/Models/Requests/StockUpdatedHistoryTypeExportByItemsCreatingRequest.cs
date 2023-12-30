namespace DataAccess.Models.Requests
{
    public class StockUpdatedHistoryTypeExportByItemsCreatingRequest
    {
        public string? Note { get; set; }

        public List<ScheduledTime> ScheduledTimes { get; set; }

        public Guid? AidRequestId { get; set; }

        //public Guid? ActivityId { get; set; }

        public List<DeliveryItemRequest> ExportedItems { get; set; }
    }
}
