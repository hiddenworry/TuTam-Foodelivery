namespace DataAccess.Models.Requests
{
    public class StockUpdatedHistoryTypeExportByStocksCreatingRequest
    {
        public string? Note { get; set; }

        //public Guid? ActivityId { get; set; }

        public List<StockRequest> ExportedStocks { get; set; }
    }
}
