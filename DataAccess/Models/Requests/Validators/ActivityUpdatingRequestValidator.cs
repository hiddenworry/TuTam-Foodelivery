using DataAccess.EntityEnums;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class ActivityUpdatingRequestValidator : AbstractValidator<ActivityUpdatingRequest>
    {
        private readonly IConfiguration _config;

        private const int MAX_IMAGES = 5;
        private const int MAX_ACTIVITY_DURATION_AS_MONTH = 3;

        public ActivityUpdatingRequestValidator(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(a => a.Id)
                .NotNull()
                .WithMessage("Id hoạt động không được bỏ trống.")
                .NotEmpty()
                .WithMessage("Id hoạt động không được bỏ trống.");

            RuleFor(a => a.Name)
                .NotNull()
                .WithMessage("Tên hoạt động không được bỏ trống.")
                .MinimumLength(5)
                .WithMessage("Tên hoạt động phải từ 5 đến 100 kí tự.")
                .MaximumLength(100)
                .WithMessage("Tên hoạt động phải từ 5 đến 100 kí tự.");

            RuleFor(a => a.Address)
                .Must(
                    address =>
                        address != null ? (address.Length >= 10 && address.Length <= 250) : true
                )
                .WithMessage("Địa chỉ hoạt động phải từ 10 đến 250 kí tự nếu có giá trị.");

            RuleFor(a => a.Location)
                .Must(l => l != null ? l.Count == 2 : true)
                .WithMessage("Vị trí phải gồm 2 giá trị vĩ độ và kinh độ nếu có giá trị.");

            RuleFor(a => a.EstimatedStartDate)
                .NotNull()
                .WithMessage($"Ngày ước tính bắt đầu không được bỏ trống.");

            RuleFor(a => a.EstimatedEndDate)
                .NotNull()
                .WithMessage($"Ngày ước tính kết thúc không được bỏ trống.")
                .GreaterThanOrEqualTo(a => a.EstimatedStartDate)
                .WithMessage(
                    $"Ngày ước tính kết thúc phải từ (hoặc bằng) ngày ước tính bắt đầu đến (hoặc bằng) {MAX_ACTIVITY_DURATION_AS_MONTH} tháng sau."
                )
                .LessThanOrEqualTo(
                    a => a.EstimatedStartDate.AddMonths(MAX_ACTIVITY_DURATION_AS_MONTH)
                )
                .WithMessage(
                    $"Ngày ước tính kết thúc phải từ (hoặc bằng) ngày ước tính bắt đầu đến (hoặc bằng) {MAX_ACTIVITY_DURATION_AS_MONTH} tháng sau."
                );

            RuleFor(a => a.DeliveringDate)
                .Must(
                    (model, deliveringDate) =>
                        deliveringDate == null
                        || deliveringDate >= model.EstimatedStartDate
                            && deliveringDate <= model.EstimatedEndDate
                )
                .WithMessage(
                    $"Ngày giao phải từ (hoặc bằng) ngày ước tính bắt đầu đến (hoặc bằng) ngày ước tính kết thúc nếu có giá trị."
                );

            RuleFor(a => a.Status)
                .NotNull()
                .Must(
                    status =>
                        status == ActivityStatus.NOT_STARTED
                        || status == ActivityStatus.STARTED
                        || status == ActivityStatus.ENDED
                )
                .WithMessage(
                    "Trạng thái hoạt động phải là NOT_STARTED(0), STARTED(1) hoặc ENDED(2)."
                );

            RuleFor(a => a.Description)
                .NotNull()
                .WithMessage("Mô tả không được để trống.")
                .MinimumLength(50)
                .WithMessage("Mô tả phải từ 50 kí tự.");

            RuleFor(a => a.Images)
                .Must(
                    images =>
                        images != null
                            ? images.Count <= MAX_IMAGES
                                && images.All(i => HaveValidImageExtension(i))
                            : true
                )
                .WithMessage(
                    $"Tối đa {MAX_IMAGES} ảnh vả các ảnh đều phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(a => a.ActivityTypeIds)
                .NotNull()
                .WithMessage("Loại hoạt động không được bỏ trống.")
                .NotEmpty()
                .WithMessage("Loại hoạt động không được bỏ trống.")
                .Must(ids => ids != null && !ids.GroupBy(id => id).Any(g => g.Count() > 1))
                .WithMessage("Chứa loại hoạt động bị trùng.");

            RuleFor(a => a.BranchIds)
                .Must(ids => ids == null || !ids.GroupBy(id => id).Any(g => g.Count() > 1))
                .WithMessage("Chứa chi nhánh bị trùng.");
        }

        private bool HaveValidImageExtension(IFormFile file)
        {
            if (file == null)
            {
                return false;
            }
            string[] allowedImageExtensions = _config
                .GetSection("FileUpload:AllowedImageExtensions")
                .Get<string[]>();
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedImageExtensions.Contains(fileExtension))
            {
                return false;
            }

            int maxFileSizeMegaBytes = _config.GetValue<int>("FileUpload:MaxFileSizeMegaBytes");
            if (file.Length == 0 || file.Length > maxFileSizeMegaBytes * 1024 * 1024)
            {
                return false;
            }
            return true;
        }
    }
}
