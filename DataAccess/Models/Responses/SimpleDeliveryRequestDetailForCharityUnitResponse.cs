using DataAccess.Models.Requests;

namespace DataAccess.Models.Responses
{
    public class SimpleDeliveryRequestDetailForCharityUnitResponse
    {
        public Guid Id { get; set; }

        public ScheduledTime? CurrentScheduledTime { get; set; }

        public DateTime? ExportedDate { get; set; }

        public string? ExportNote { get; set; }

        public string? ProofImage { get; set; }

        public string Avatar { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public bool IsReported { get; set; }

        public ReportResponse? Report { get; set; }

        public Guid BranchId { get; set; }

        public string BranchName { get; set; }

        public string BranchAddress { get; set; }

        public string BranchImage { get; set; }

        public List<FinishedDeliveryItemTypeExportResponse> DeliveryItems { get; set; }
    }
}
