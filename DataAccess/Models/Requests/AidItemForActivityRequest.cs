using DataAccess.Models.Requests.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Models.Requests
{
    [ModelBinder(BinderType = typeof(MetadataValueModelBinder))]
    public class AidItemForActivityRequest
    {
        public Guid AidItemId { get; set; }

        public double Quantity { get; set; }
    }
}
