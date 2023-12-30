using DataAccess.Models.Requests.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Models.Requests
{
    [ModelBinder(BinderType = typeof(MetadataValueModelBinder))]
    public class DonatedItemRequest
    {
        public Guid ItemTemplateId { get; set; }

        public double Quantity { get; set; }

        public DateTime InitialExpirationDate { get; set; }
    }
}
