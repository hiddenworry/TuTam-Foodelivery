using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class DirectDonationRequestValidation : AbstractValidator<DirectDonationRequest>
    {
        public DirectDonationRequestValidation()
        {
            RuleFor(request => request.Quantity)
                .GreaterThan(0)
                .WithMessage("Số lượng phải lớn hơn 0.");

            RuleFor(request => request.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú phải có tối đa 500 kí tự nếu có.");

            RuleFor(request => request.ExpirationDate)
                .Must(BeValidExpirationDate)
                .WithMessage(
                    "Bạn chỉ có thể nhập vật phẩm có hạn sử dụng ít nhất 2 ngày kể từ hiện tại."
                );
        }

        private bool BeValidExpirationDate(DateTime expirationDate)
        {
            DateTime currentDate = DateTime.Today;
            DateTime minDate = currentDate.AddDays(2);

            return expirationDate >= minDate;
        }
    }
}
