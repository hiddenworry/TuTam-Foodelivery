using DataAccess.Models.Requests.Validators.Common;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class StockUpdatedHistoryTypeExportByItemsCreatingRequestValidator
        : AbstractValidator<StockUpdatedHistoryTypeExportByItemsCreatingRequest>
    {
        public StockUpdatedHistoryTypeExportByItemsCreatingRequestValidator()
        {
            RuleFor(e => e.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú cho xuất kho tối đa 500 ký tự.");

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

            //RuleFor(e => e.AidRequestId)
            //    .NotNull()
            //    .WithMessage("Id của yêu cầu hỗ trợ vật phẩm không được trống.");

            RuleFor(ar => ar.ExportedItems)
                .NotNull()
                .WithMessage("Danh sách vật phẩm vận chuyển không được trống.")
                .NotEmpty()
                .WithMessage("Danh sách vật phẩm vận chuyển không được trống.")
                .Must(
                    dis =>
                        dis != null
                        && dis.Count > 0
                        && !dis.GroupBy(item => item.ItemId).Any(g => g.Count() > 1)
                        && dis.All(di => di.Quantity > 0)
                )
                .WithMessage(
                    "Danh sách vật phẩm vận chuyển không được trống, trùng cho và số lượng vận chuyển của vật phẩm phải lớn hơn 0 cho mỗi yêu cầu vận chuyển."
                )
                .Must(dis => dis != null && dis.All(di => di.Note == null || di.Note.Length <= 500))
                .WithMessage(
                    "Danh sách ghi chú cho từng lượng kho lấy ra để xuất tối đa 500 ký tự."
                );
        }
    }
}
