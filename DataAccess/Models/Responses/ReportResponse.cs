namespace DataAccess.Models.Responses
{
    public class ReportResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Type { get; set; }
    }
}
