namespace DataAccess.Models.Responses
{
    public class SimpleStockUpdatedHistoryForCharityUnitResponse
    {
        public Guid Id { get; set; }

        public DateTime? ExportedDate { get; set; }

        public string? ExportNote { get; set; }
    }
}
