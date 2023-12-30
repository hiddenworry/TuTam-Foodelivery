namespace DataAccess.Models.Responses
{
    public class UserPermissionResponse
    {
        public Guid PermissionId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; }
        public string? Name { get; set; }

        public string? DisplayName { get; set; }
    }
}
