namespace DataAccess.Models.Responses
{
    public class TotalStockOfAnItemResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string? Note { get; set; }

        public int EstimatedExpirationDays { get; set; }

        public double MaximumTransportVolume { get; set; }

        public string Unit { get; set; }

        public double TotalStock { get; set; }
    }
}
