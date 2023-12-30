namespace DataAccess.Models.Responses
{
    public class CharityUnitForAdminResponse
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Phone { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string LegalDocuments { get; set; }
        public string Location { get; set; }
        public string Address { get; set; }
        public bool isWatingToConfirmUpdate { get; set; }
        public bool? isHeadQuater { get; set; }
    }
}
