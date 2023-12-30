namespace DataAccess.Models.Responses
{
    public class TargetProcessResponse
    {
        public double Target { get; set; }

        public double Process { get; set; }

        public ItemResponse ItemTemplateResponse { get; set; }
    }
}
