namespace DataAccess.Models.Responses
{
    public class DonatedItemResponse
    {
        public Guid Id { get; set; }

        public double Quantity { get; set; }

        public double ImportedQuantity { get; set; }

        public DateTime InitialExpirationDate { get; set; }

        public string Status { get; set; }

        public ItemResponse ItemTemplateResponse { get; set; }
    }
}
