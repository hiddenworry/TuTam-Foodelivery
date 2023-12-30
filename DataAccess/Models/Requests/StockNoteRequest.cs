namespace DataAccess.Models.Requests
{
    public class StockNoteRequest
    {
        public Guid StockId { get; set; }

        public string? Note { get; set; }
    }
}
