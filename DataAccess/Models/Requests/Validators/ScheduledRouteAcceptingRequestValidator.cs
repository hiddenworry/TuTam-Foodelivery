using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ScheduledRouteAcceptingRequestValidator
        : AbstractValidator<ScheduledRouteAcceptingRequest>
    {
        public ScheduledRouteAcceptingRequestValidator()
        {
            RuleFor(sr => sr.ScheduledRouteId)
                .NotNull()
                .WithMessage("Id lịch trình vận chuyển không được trống.");
        }
    }
}
