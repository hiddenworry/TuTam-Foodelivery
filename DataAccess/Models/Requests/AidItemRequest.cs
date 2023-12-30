using DataAccess.Models.Requests.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Requests
{
    [ModelBinder(BinderType = typeof(MetadataValueModelBinder))]
    public class AidItemRequest
    {
        public Guid ItemTemplateId { get; set; }

        [Required]
        public double Quantity { get; set; }
    }
}
