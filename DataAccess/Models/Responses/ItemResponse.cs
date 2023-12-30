namespace DataAccess.Models.Responses
{
    public class ItemResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<string> AttributeValues { get; set; }

        public string Image { get; set; }

        public string? Note { get; set; }

        public int EstimatedExpirationDays { get; set; }

        public double MaximumTransportVolume { get; set; }

        public string Unit { get; set; }

        public ItemCategoryResponse? CategoryResponse { get; set; }
    }
}
