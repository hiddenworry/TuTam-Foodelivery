namespace DataAccess.Models.Responses
{
    public class AidItemForActivityResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public double Quantity { get; set; }

        public string Unit { get; set; }

        public string UrgentLevel { get; set; }

        public string CharityUnit { get; set; }

        public List<DateTime> AidPeriod { get; set; }
    }
}
