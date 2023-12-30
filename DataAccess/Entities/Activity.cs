using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class Activity
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        public string Name { get; set; }

        [StringLength(250, MinimumLength = 10)]
        public string? Address { get; set; }

        public string? Location { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime EstimatedStartDate { get; set; }

        public DateTime EstimatedEndDate { get; set; }

        public DateTime? DeliveringDate { get; set; }

        public ActivityStatus Status { get; set; }

        [MinLength(50)]
        [Required]
        public string Description { get; set; }

        [Required]
        public string Images { get; set; }

        public ActivityScope Scope { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public List<ActivityTypeComponent> ActivityTypeComponents { get; set; }

        public List<TargetProcess> TargetProcesses { get; set; }

        public List<ActivityMember> ActivityMembers { get; set; }

        public List<ActivityFeedback> ActivityFeedbacks { get; set; }

        public List<Phase> Phases { get; set; }

        public List<ActivityRole> ActivityRoles { get; set; }

        public List<ActivityBranch> ActivityBranches { get; set; }

        public List<DonatedRequest> DonatedRequests { get; set; }

        public List<Stock> Stocks { get; set; }
    }
}
