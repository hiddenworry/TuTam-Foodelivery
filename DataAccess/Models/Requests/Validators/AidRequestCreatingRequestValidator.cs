using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Requests.Validators.Common;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class AidRequestCreatingRequestValidator : AbstractValidator<AidRequestCreatingRequest>
    {
        private const int ESTIMATED_START_MAX_MONTHS_LATER = 3;

        public AidRequestCreatingRequestValidator()
        {
            RuleFor(ar => ar.ScheduledTimes)
                .NotNull()
                .NotEmpty()
                .Must(sts => sts != null && sts.All(st => CommonValidator.IsScheduledTimeValid(st)))
                .WithMessage(
                    $"Khung giờ nhận phải cách nhau ít nhất {CommonValidator.MIN_PERIOD_AS_HOUR} giờ."
                )
                .Must(
                    sts =>
                        sts.All(
                            st =>
                                CommonValidator.IsScheduledTimeValid(st)
                                && DateOnly.Parse(st.Day)
                                    >= DateOnly
                                        .FromDateTime(SettedUpDateTime.GetCurrentVietNamTime())
                                        .AddDays(2)
                                && DateOnly.Parse(st.Day)
                                    <= DateOnly
                                        .FromDateTime(SettedUpDateTime.GetCurrentVietNamTime())
                                        .AddMonths(ESTIMATED_START_MAX_MONTHS_LATER)
                        )
                )
                .WithMessage(
                    $"Các ngày có thể nhận phải từ 2 ngày sau đến {ESTIMATED_START_MAX_MONTHS_LATER} tháng sau."
                )
                .Must(
                    sts =>
                        sts != null
                        && sts.All(st => CommonValidator.IsScheduledTimeValid(st))
                        && CommonValidator.IsScheduledTimesNotDuplicate(sts)
                )
                .WithMessage($"Các ngày có thể nhận không được trùng ngày hoặc cả ngày và giờ.");

            RuleFor(ar => ar.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú phải có tối đa 500 kí tự nếu có.");

            RuleFor(ar => ar.AidItemRequests)
                .NotNull()
                .WithMessage("Danh sách vật phẩm cần hỗ trợ không được trống.")
                .NotEmpty()
                .WithMessage("Danh sách vật phẩm cần hỗ trợ không được trống.")
                .Must(
                    items =>
                        items != null
                        && !items.GroupBy(item => item.ItemTemplateId).Any(g => g.Count() > 1)
                )
                .WithMessage("Danh sách chứa vật phẩm cần hỗ trợ bị trùng.")
                .Must(items => !items.Any(item => item.Quantity < 1))
                .WithMessage("Danh sách chứa vật phẩm cần hỗ trợ có số lượng bé hơn 1.");
        }
    }
}
