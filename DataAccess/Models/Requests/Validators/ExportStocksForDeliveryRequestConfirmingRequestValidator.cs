using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ExportStocksForDeliveryRequestConfirmingRequestValidator
        : AbstractValidator<ExportStocksForDeliveryRequestConfirmingRequest>
    {
        public ExportStocksForDeliveryRequestConfirmingRequestValidator()
        {
            RuleFor(e => e.ScheduledRouteId)
                .NotNull()
                .WithMessage("Id lịch trình vận chuyển không được trống.");

            RuleFor(e => e.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú cho xuất kho tối đa 500 ký tự.");

            RuleFor(e => e.NotesOfStockUpdatedHistoryDetails)
                .Must(
                    ns =>
                        ns == null
                        || ns.All(n => n != null && (n.Note == null || n.Note.Length <= 500))
                )
                .WithMessage(
                    "Danh sách ghi chú cho từng lượng kho lấy ra để xuất tối đa 500 ký tự."
                );
        }
    }
}
