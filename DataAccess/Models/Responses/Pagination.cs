namespace DataAccess.Models.Responses
{
    public class Pagination
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public long Total { get; set; }
    }
}
