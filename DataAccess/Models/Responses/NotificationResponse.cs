namespace DataAccess.Models.Responses
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Image { get; set; }

        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ReceiverId { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public string DataType { get; set; }

        public Guid? DataId { get; set; }
    }
}
