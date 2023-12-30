namespace DataAccess.Models.Requests
{
    public class ExportStocksForDeliveryRequestConfirmingRequest
    {
        public Guid ScheduledRouteId { get; set; }

        public string? Note { get; set; }

        public List<StockNoteRequest>? NotesOfStockUpdatedHistoryDetails { get; set; }
    }
}
