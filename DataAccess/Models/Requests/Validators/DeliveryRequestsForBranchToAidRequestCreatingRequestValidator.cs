﻿using DataAccess.Models.Requests.Validators.Common;
using FluentValidation;

namespace DataAccess.Models.Requests.Validators
{
    public class DeliveryRequestsForBranchToAidRequestCreatingRequestValidator
        : AbstractValidator<DeliveryRequestsForBranchToAidRequestCreatingRequest>
    {
        public DeliveryRequestsForBranchToAidRequestCreatingRequestValidator()
        {
            RuleFor(ar => ar.Note)
                .Must(n => n == null || n.Length <= 500)
                .WithMessage("Ghi chú phải có tối đa 500 kí tự nếu có.");

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

            RuleFor(ar => ar.AidRequestId)
                .NotNull()
                .WithMessage("Id yêu cầu hỗ trợ vật phẩm không được trống.");

            RuleFor(ar => ar.DeliveryItemsForDeliveries)
                .NotNull()
                .WithMessage("Danh sách vật phẩm vận chuyển không được trống.")
                .NotEmpty()
                .WithMessage("Danh sách vật phẩm vận chuyển không được trống.")
                .Must(
                    disl =>
                        disl != null
                        && disl.Count > 0
                        && disl.All(
                            dis =>
                                dis != null
                                && dis.Count > 0
                                && !dis.GroupBy(item => item.ItemId).Any(g => g.Count() > 1)
                                && dis.All(di => di.Quantity > 0)
                        )
                )
                .WithMessage(
                    "Danh sách vật phẩm vận chuyển không được trống, trùng cho và số lượng vận chuyển của vật phẩm phải lớn hơn 0 cho mỗi yêu cầu vận chuyển."
                );
        }
    }
}
