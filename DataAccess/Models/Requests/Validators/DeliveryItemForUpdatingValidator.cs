using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class DeliveryItemForUpdatingValidator : AbstractValidator<DeliveryItemForUpdating>
    {
        public DeliveryItemForUpdatingValidator()
        {
            RuleFor(di => di.DeliveryItemId).NotNull().WithMessage("Id vật phẩm không được trống.");

            RuleFor(di => di.Quantity)
                .Must(q => q >= 0)
                .WithMessage("Số lượng vật phẩm nhận không được bé hơn 0.");
        }
    }
}
