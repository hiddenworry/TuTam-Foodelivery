namespace DataAccess.Models.Responses
{
    public class DeliveryPointResponseDetails
    {
        public Guid? UserId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? CharityUnitId { get; set; }

        public string Name { get; set; }

        public string? Avatar { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string Address { get; set; }

        public string Location { get; set; }
    }
}
