namespace DataAccess.Models.Responses
{
    public class CommonResponse
    {
        public int Status { get; set; }

        public object? Data { get; set; }

        public Pagination? Pagination { get; set; }

        public string? Message { get; set; }
    }
}
