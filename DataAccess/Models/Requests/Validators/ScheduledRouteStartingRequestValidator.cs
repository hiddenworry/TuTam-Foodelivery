using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ScheduledRouteStartingRequestValidator
        : AbstractValidator<ScheduledRouteStartingRequest>
    {
        public ScheduledRouteStartingRequestValidator()
        {
            RuleFor(sr => sr.ScheduledRouteId)
                .NotNull()
                .WithMessage("Id lịch trình vận chuyển không được trống.");

            RuleFor(sr => sr.Latitude).NotNull().WithMessage("Vĩ độ không được trống.");

            RuleFor(sr => sr.Longitude).NotNull().WithMessage("Kinh độ không được trống.");
        }
    }
}
