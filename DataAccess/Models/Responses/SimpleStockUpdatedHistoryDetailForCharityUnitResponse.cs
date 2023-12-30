namespace DataAccess.Models.Responses
{
    public class SimpleStockUpdatedHistoryDetailForCharityUnitResponse
    {
        public Guid Id { get; set; }

        public DateTime? ExportedDate { get; set; }

        public string? ExportedNote { get; set; }

        public Guid BranchId { get; set; }

        public string BranchName { get; set; }

        public string BranchAddress { get; set; }

        public string BranchImage { get; set; }

        public List<StockUpdatedHistoryDetailForSelfShippingResponse> ExportedItems { get; set; }
    }
}
