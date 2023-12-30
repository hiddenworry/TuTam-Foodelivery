using DataAccess.Models.Requests.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Models.Requests
{
    [ModelBinder(BinderType = typeof(MetadataValueModelBinder))]
    public class TargetProcessRequest
    {
        public Guid ItemId { get; set; }

        public double Quantity { get; set; }
    }
}
