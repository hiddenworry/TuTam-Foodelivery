using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class AidRequestComfirmingRequestValidator
        : AbstractValidator<AidRequestComfirmingRequest>
    {
        public AidRequestComfirmingRequestValidator()
        {
            RuleFor(arcr => arcr.Id)
                .NotNull()
                .WithMessage("Id yêu cầu cần được hỗ trợ không được bỏ trống.")
                .NotEmpty()
                .WithMessage("Id yêu cầu cần được hỗ trợ không được bỏ trống.");

            RuleFor(arcr => arcr.RejectingReason)
                .Must(r => r == null || (r.Length >= 25 && r.Length <= 500))
                .WithMessage(
                    "Chỉ khi không nhận ít nhất 1 vật phẩm hỗ trợ thì mới truyền lý do từ chối và phải có từ 25 đến 500 kí tự."
                );
        }
    }
}
