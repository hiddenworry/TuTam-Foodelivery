using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class StockUpdatedHistoryTypeExportByStocksCreatingRequestValidator
        : AbstractValidator<StockUpdatedHistoryTypeExportByStocksCreatingRequest>
    {
        public StockUpdatedHistoryTypeExportByStocksCreatingRequestValidator()
        {
            RuleFor(e => e.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú cho xuất kho tối đa 500 ký tự.");

            RuleFor(ar => ar.ExportedStocks)
                .NotNull()
                .WithMessage("Danh sách vật phẩm xuất không được trống.")
                .NotEmpty()
                .WithMessage("Danh sách vật phẩm xuất chuyển không được trống.")
                .Must(
                    dis =>
                        dis != null
                        && dis.Count > 0
                        && !dis.GroupBy(item => item.StockId).Any(g => g.Count() > 1)
                        && dis.All(di => di.Quantity > 0)
                )
                .WithMessage(
                    "Danh sách vật phẩm xuất không được trống, trùng cho và số lượng xuất của vật phẩm phải lớn hơn 0."
                )
                .Must(dis => dis != null && dis.All(di => di.Note == null || di.Note.Length <= 500))
                .WithMessage(
                    "Danh sách ghi chú cho từng lượng kho lấy ra để xuất tối đa 500 ký tự."
                );
        }
    }
}
