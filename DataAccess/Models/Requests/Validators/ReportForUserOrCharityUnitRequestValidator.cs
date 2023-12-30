using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ReportForUserOrCharityUnitRequestValidator
        : AbstractValidator<ReportForUserOrCharityUnitRequest>
    {
        public ReportForUserOrCharityUnitRequestValidator()
        {
            RuleFor(r => r.Title)
                .Must(t => t != null && t.Length >= 10 && t.Length <= 150)
                .WithMessage("Tiêu đề phải từ 10 đến 150 ký tự.");

            RuleFor(r => r.Content)
                .Must(t => t != null && t.Length >= 10 && t.Length <= 150)
                .WithMessage("Nội dung phải từ 10 đến 150 ký tự.");
        }
    }
}
