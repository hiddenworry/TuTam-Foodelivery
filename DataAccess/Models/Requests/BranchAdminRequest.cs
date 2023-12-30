using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    public class BranchAdminRequest
    {
        [Required]
        public Guid UserId { get; set; }

        //[Required]
        //[Range(
        //    0,
        //    1,
        //    ErrorMessage = "Giá trị phải là 0 hoặc 1, tương ứng với ACTIVE hoặc INACTIVE."
        //)]
        //public MemberStatus status { get; set; }
    }
}
