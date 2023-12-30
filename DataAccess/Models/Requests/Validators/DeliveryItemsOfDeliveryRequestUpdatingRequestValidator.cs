using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class DeliveryItemsOfDeliveryRequestUpdatingRequestValidator
        : AbstractValidator<DeliveryItemsOfDeliveryRequestUpdatingRequest>
    {
        public DeliveryItemsOfDeliveryRequestUpdatingRequestValidator()
        {
            RuleFor(dis => dis.ProofImage)
                .Must(i => !string.IsNullOrWhiteSpace(i))
                .WithMessage("Ảnh khi nhận vật phẩm không được trống.");

            RuleFor(dis => dis.DeliveryItemForUpdatings)
                .Must(dis => dis != null && dis.Count > 0)
                .WithMessage("Danh sách vật phẩm nhận không được trống.");
        }
    }
}
