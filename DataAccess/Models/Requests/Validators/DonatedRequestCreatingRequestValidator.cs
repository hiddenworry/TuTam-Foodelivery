using DataAccess.Models.Requests.ModelBinders;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class DonatedRequestCreatingRequestValidator
        : AbstractValidator<DonatedRequestCreatingRequest>
    {
        private readonly IConfiguration _config;

        private const int MAX_IMAGES = 5;
        private const int MAX_DAYS_LATER_FOR_PICKING_UP = 14;
        private const int MIN_HOURS_FOR_PICKING_UP = 1;
        private const int MIN_HOUR_LATER_FOR_PICKING_UP = 1;

        public DonatedRequestCreatingRequestValidator(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(drcr => drcr.Images)
                .NotNull()
                .WithMessage("Ảnh không được bỏ trống.")
                .NotEmpty()
                .WithMessage("Ảnh không được bỏ trống.")
                .Must(
                    images =>
                        images != null
                        && images.Count <= MAX_IMAGES
                        && images.All(i => HaveValidImageExtension(i))
                )
                .WithMessage(
                    $"Tối đa {MAX_IMAGES} ảnh vả các ảnh đều phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(a => a.Address)
                .Must(address => address != null && address.Length >= 10 && address.Length <= 250)
                .WithMessage("Địa chỉ nơi quyên góp phải từ 10 đến 250 kí tự.");

            RuleFor(a => a.Location)
                .Must(l => l != null && l.Count == 2)
                .WithMessage("Vị trí nơi quyên góp phải gồm 2 giá trị vĩ độ và kinh độ.");

            RuleFor(ar => ar.ScheduledTimes)
                .NotNull()
                .NotEmpty()
                .Must(sts => sts != null && sts.All(st => IsScheduledTimeValid(st)))
                .WithMessage(
                    $"Các ngày có thể quyên góp phải từ {MIN_HOUR_LATER_FOR_PICKING_UP} giờ sau đến {MAX_DAYS_LATER_FOR_PICKING_UP} ngày sau và khung giờ cho phải cách nhau ít nhất {MIN_HOURS_FOR_PICKING_UP} tiếng."
                );

            RuleFor(ar => ar.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú phải có tối đa 500 kí tự nếu có.");

            RuleFor(ar => ar.DonatedItemRequests)
                .NotNull()
                .WithMessage("Danh sách vật phẩm quyên góp không được trống.")
                .NotEmpty()
                .WithMessage("Danh sách vật phẩm quyên góp không được trống.")
                .Must(
                    items =>
                        items != null
                        && !items.GroupBy(item => item.ItemTemplateId).Any(g => g.Count() > 1)
                )
                .WithMessage("Danh sách chứa vật phẩm quyên góp bị trùng.")
                .Must(items => items != null && !items.Any(item => item.Quantity < 1))
                .WithMessage("Danh sách chứa vật phẩm quyên góp có số lượng bé hơn 1.")
                .Must(
                    (model, items) =>
                        items != null
                        && !items.Any(
                            item =>
                                item.InitialExpirationDate
                                < DateOnly
                                    .FromDateTime(
                                        GetEndDateTimeFromScheduledTime(
                                            GetLastAvailabeScheduledTime(model.ScheduledTimes)!
                                        )
                                    )
                                    .AddDays(2)
                                    .ToDateTime(TimeOnly.MinValue)
                        )
                //&& !items.Any(
                //    item =>
                //        item.InitialExpirationDate
                //        <= DateOnly
                //            .FromDateTime(SettedUpDateTime.GetCurrentVietNamTime())
                //            .AddDays(2)
                //            .ToDateTime(TimeOnly.MinValue)
                //)
                )
                .WithMessage("Hạn sử dụng ước tính phải từ 2 ngày sau.");
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

        private bool IsScheduledTimeValid(ScheduledTime scheduledTime)
        {
            if (
                !(
                    DateOnly.TryParse(scheduledTime.Day, out _)
                    && TimeOnly.TryParse(scheduledTime.StartTime, out _)
                    && TimeOnly.TryParse(scheduledTime.EndTime, out _)
                )
            )
            {
                return false;
            }

            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);

            if (
                day.ToDateTime(startTime)
                <= SettedUpDateTime.GetCurrentVietNamTime().AddHours(MIN_HOUR_LATER_FOR_PICKING_UP)
            )
            {
                return false;
            }

            if (endTime - startTime < TimeSpan.FromHours(MIN_HOURS_FOR_PICKING_UP))
            {
                return false;
            }

            if (
                day.ToDateTime(endTime)
                >= SettedUpDateTime.GetCurrentVietNamTime().AddDays(MAX_DAYS_LATER_FOR_PICKING_UP)
            )
            {
                return false;
            }

            return true;
        }

        private DateTime GetEndDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
            return day.ToDateTime(endTime);
        }

        private ScheduledTime? GetLastAvailabeScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes
                .Where(
                    st =>
                        GetEndDateTimeFromScheduledTime(st)
                        > SettedUpDateTime.GetCurrentVietNamTime()
                )
                .MaxBy(st => GetEndDateTimeFromScheduledTime(st));
        }
    }
}
