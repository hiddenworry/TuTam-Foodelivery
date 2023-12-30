namespace DataAccess.Models.Responses
{
    public class CharityResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status { get; set; }

        public string Logo { get; set; }

        public string Description { get; set; }

        public int NumberOfCharityUnits { get; set; }

        public bool isWattingToUpdate { get; set; }
    }
}
