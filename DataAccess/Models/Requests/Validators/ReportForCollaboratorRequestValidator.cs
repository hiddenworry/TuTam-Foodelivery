using DataAccess.EntityEnums;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ReportForCollaboratorRequestValidator
        : AbstractValidator<ReportForContributorRequest>
    {
        public ReportForCollaboratorRequestValidator()
        {
            RuleFor(r => r.Title)
                .Must(t => t != null && t.Length >= 10 && t.Length <= 150)
                .WithMessage("Tiêu đề phải từ 10 đến 150 ký tự.");

            RuleFor(r => r.Content)
                .Must(t => t != null && t.Length >= 10 && t.Length <= 150)
                .WithMessage("Nội dung phải từ 10 đến 150 ký tự.");

            RuleFor(r => r.Type)
                .Must(
                    t =>
                        t == ReportType.CONTRIBUTOR_DO_NOT_GIVE_ITEMS
                        || t == ReportType.CONTRIBUTOR_ALREADY_GIVEN_ALL_ITEMS
                )
                .WithMessage(
                    "Loại báo cáo phải là 2 (người quyên góp không giao đồ) hoặc 3 (người dùng đã cho hết đồ)."
                );
        }
    }
}
