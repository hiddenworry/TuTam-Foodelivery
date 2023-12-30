using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    internal class DonatedRequestConfirmingRequestValidator
        : AbstractValidator<DonatedRequestConfirmingRequest>
    {
        public DonatedRequestConfirmingRequestValidator()
        {
            RuleFor(drcr => drcr.Id)
                .NotNull()
                .WithMessage("Id yêu cầu quyên góp không được bỏ trống.")
                .NotEmpty()
                .WithMessage("Id yêu cầu quyên góp không được bỏ trống.");

            RuleFor(drcr => drcr.RejectingReason)
                .Must(r => r == null || r.Length >= 25 && r.Length <= 500)
                .WithMessage(
                    "Chỉ khi không nhận ít nhất 1 vật phẩm thì mới truyền lý do từ chối và phải có từ 25 đến 500 kí tự."
                );
        }
    }
}
