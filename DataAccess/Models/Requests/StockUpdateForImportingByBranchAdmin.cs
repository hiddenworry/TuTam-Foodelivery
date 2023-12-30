using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class StockUpdateForImportingByBranchAdmin
    {
        [MinLength(1)]
        public List<DirectDonationRequest> DirectDonationRequests { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú phải có tối đa 500 kí tự.")]
        public string? Note { get; set; }
    }
}
