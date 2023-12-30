using DataAccess.EntityEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataAccess.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100, MinimumLength = 5)]
        public string Email { get; set; }

        public string Password { get; set; }

        [StringLength(11)]
        [Required]
        public string Phone { get; set; }

        [StringLength(50, MinimumLength = 5)]
        [Required]
        public string? Name { get; set; }

        [StringLength(250, MinimumLength = 10)]
        public string? Address { get; set; }

        public string? Location { get; set; }

        [Required]
        public UserStatus Status { get; set; }

        public string? Avatar { get; set; }

        [JsonIgnore]
        public string? AccessToken { get; set; }

        [JsonIgnore]
        public string? RefreshToken { get; set; }

        [JsonIgnore]
        public string? DeviceToken { get; set; }

        public DateTime? RefreshTokenExpiration { get; set; }

        [JsonIgnore]
        public string? VerifyCode { get; set; }

        public DateTime? VerifyCodeExpiration { get; set; }

        [JsonIgnore]
        public string? OtpCode { get; set; }

        public DateTime? OtpCodeExpiration { get; set; }

        [ForeignKey(nameof(Role))]
        public Guid RoleId { get; set; }

        public Role Role { get; set; }

        public List<UserPermission> UserPermissions { get; set; }

        public List<Report> Reports { get; set; }

        public List<DonatedRequest> DonatedRequests { get; set; }

        public List<ActivityMember> ActivityMembers { get; set; }

        public List<ActivityFeedback> ActivityFeedbacks { get; set; }

        public List<PostComment> PostComments { get; set; }

        public List<ScheduledRoute> ScheduledRoutes { get; set; }

        [JsonIgnore]
        public Branch? Branch { get; set; }

        [JsonIgnore]
        public List<CharityUnit>? CharityUnit { get; set; }

        [JsonIgnore]
        public CollaboratorApplication? CollaboratorApplication { get; set; }

        public bool IsCollaborator { get; set; }

        public List<Post> Posts { get; set; }

        public List<Stock> Stocks { get; set; }
    }
}
