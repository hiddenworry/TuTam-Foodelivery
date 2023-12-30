using DataAccess.EntityEnums;

namespace DataAccess.Models.Requests
{
    public class UserFilterRequest
    {
        public string? KeyWord { get; set; }

        public List<Guid>? RoleIds { get; set; }

        public UserStatus? UserStatus { get; set; }
    }
}
