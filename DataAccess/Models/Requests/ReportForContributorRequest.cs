using DataAccess.EntityEnums;

namespace DataAccess.Models.Requests
{
    public class ReportForContributorRequest
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public ReportType Type { get; set; }
    }
}
