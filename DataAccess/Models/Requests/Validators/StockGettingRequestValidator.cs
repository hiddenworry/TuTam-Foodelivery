using DataAccess.Models.Requests.Validators.Common;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class StockGettingRequestValidator : AbstractValidator<StockGettingRequest>
    {
        public StockGettingRequestValidator()
        {
            RuleFor(s => s.ItemId).NotNull().WithMessage("Id vật phẩm không được trống.");

            RuleFor(s => s.ScheduledTimes)
                .NotNull()
                .NotEmpty()
                .Must(
                    sts =>
                        sts != null
                        && sts.All(st => CommonValidator.IsScheduledTimeValidForDelivery(st))
                )
                .WithMessage(
                    $"Các ngày vận chuyển phải từ hiện tại và khung giờ cho phải cách nhau ít nhất {CommonValidator.MIN_PERIOD_AS_HOUR} tiếng."
                )
                .Must(
                    sts =>
                        sts != null
                        && sts.All(st => CommonValidator.IsScheduledTimeValidForDelivery(st))
                        && CommonValidator.IsScheduledTimesNotDuplicate(sts)
                )
                .WithMessage(
                    $"Các ngày vận chuyển không được trùng ngày hoặc cả ngày và giờ, và phải kết thúc sau hiện tại."
                );
        }
    }
}
