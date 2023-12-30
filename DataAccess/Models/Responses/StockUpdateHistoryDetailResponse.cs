namespace DataAccess.Models.Responses
{
    public class StockUpdateHistoryDetailResponse
    {
        public Guid? Id { get; set; }

        public double? Quantity { get; set; }

        public string? Name { get; set; }

        public List<string>? AttributeValues { get; set; }

        public string? Unit { get; set; }

        public string? PickUpPoint { get; set; }

        public string? DeliveryPoint { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? Type { get; set; }

        public string? Note { get; set; }

        public string? DonorName { get; set; }

        public string? ActivityName { get; set; }
    }
}
