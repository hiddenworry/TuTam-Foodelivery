using DataAccess.Models.Requests.ModelBinders;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class ReceivedItemsToFinishScheduledRouteValidator
        : AbstractValidator<ReceivedItemsToFinishScheduledRoute>
    {
        public ReceivedItemsToFinishScheduledRouteValidator()
        {
            RuleFor(ris => ris.ScheduledRouteId)
                .NotNull()
                .WithMessage("Id lịch trình vận chuyển không được trống.");

            RuleFor(ris => ris.DeliveryRequests)
                .Must(drs => IsDeliveryRequestsValid(drs))
                .WithMessage(
                    "Thông tin vật phẩm nhận của các yêu cầu vận chuyển phải không trùng Id, danh sách vật phẩm không trùng Id, số lượng không được bé hơn 0, ghi chú tối đa 500 ký tự nếu có và HSD phải từ hiện tại trở đi."
                );
        }

        private bool IsDeliveryRequestsValid(List<DeliveryRequestRequest> deliveryRequestRequests)
        {
            if (deliveryRequestRequests == null)
                return false;

            if (deliveryRequestRequests.GroupBy(dr => dr.DeliveryRequestId).Any(g => g.Count() > 1))
                return false;

            foreach (DeliveryRequestRequest deliveryRequestRequest in deliveryRequestRequests)
            {
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
                if (
                    deliveryRequestRequest.DeliveryRequestId == null
                    || deliveryRequestRequest.ReceivedDeliveryItemRequests == null
                )
                    return false;
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

                if (
                    deliveryRequestRequest.ReceivedDeliveryItemRequests
                        .GroupBy(ri => ri.DeliveryItemId)
                        .Any(g => g.Count() > 1)
                )
                    return false;

                foreach (
                    ReceivedDeliveryItemRequest receivedDeliveryItemRequest in deliveryRequestRequest.ReceivedDeliveryItemRequests
                )
                {
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
                    if (
                        receivedDeliveryItemRequest.DeliveryItemId == null
                        || receivedDeliveryItemRequest.Quantity < 0
                        || receivedDeliveryItemRequest.ExpirationDate
                            <= SettedUpDateTime.GetCurrentVietNamTime()
                        || receivedDeliveryItemRequest.Note != null
                            && receivedDeliveryItemRequest.Note.Length > 500
                    )
                        return false;
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
                }
            }
            return true;
        }
    }
}
