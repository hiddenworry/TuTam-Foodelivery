using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class PhaseService : IPhaseService
    {
        private readonly IPhaseRepository _phaseRepository;
        private readonly ILogger<PhaseService> _logger;
        private readonly IConfiguration _config;
        private readonly IActivityRepository _activityRepository;

        public PhaseService(
            IPhaseRepository phaseRepository,
            ILogger<PhaseService> logger,
            IConfiguration configuration,
            IActivityRepository activityRepository
        )
        {
            _phaseRepository = phaseRepository;
            _config = configuration;
            _logger = logger;
            _activityRepository = activityRepository;
        }

        public async Task<CommonResponse> CreatePharse(PharseCreatingRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Activity? activity = await _activityRepository.FindActivityByIdAsync(
                    request.ActivityId
                );
                int order = 0;
                if (activity != null && activity.Status != ActivityStatus.ENDED)
                {
                    foreach (var p in request.phaseRequests)
                    {
                        order = order + 1;
                        if (p.EstimatedStartDate >= p.EstimatedEndDate)
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message = "Ngày bắt đầu phải trước ngày kết thúc";
                            return commonResponse;
                        }

                        TimeSpan timeDifference = p.EstimatedEndDate - p.EstimatedStartDate;

                        if (timeDifference.TotalMinutes < 30)
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message =
                                "Thời gian kết thúc phải cách thời gian bắt đầu ít nhất 30 phút";
                            return commonResponse;
                        }

                        Phase phase = new Phase();

                        phase.ActivityId = request.ActivityId;
                        phase.Status = PhaseStatus.NOT_STARTED;
                        phase.EstimatedStartDate = p.EstimatedStartDate;

                        phase.EstimatedEndDate = p.EstimatedEndDate;
                        phase.Name = p.Name;
                        phase.Order = order;
                        await _phaseRepository.CreatePhaseAsync(phase);
                        commonResponse.Status = 200;
                        commonResponse.Message = "Tạo thành công";
                        TimeSpan startDelay = phase.EstimatedStartDate - DateTime.Now;
                        if (startDelay.TotalMilliseconds > 0)
                        {
                            var jobId = BackgroundJob.Schedule(
                                () => StartPhase(phase.Id),
                                startDelay
                            );
                        }
                        TimeSpan endDelay = phase.EstimatedEndDate - DateTime.Now;
                        if (endDelay.TotalMilliseconds > 0)
                        {
                            var jobId = BackgroundJob.Schedule(() => EndPhase(phase.Id), endDelay);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(CreatePharse);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdatePharse(
            List<PhaseUpdatingRequest> phaseUpdatingRequest
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                foreach (var request in phaseUpdatingRequest)
                {
                    Phase? phase = await _phaseRepository.GetPhaseByIdAsync(request.Id);
                    if (phase != null)
                    {
                        if (
                            (request.EstimatedStartDate != null || request.EstimatedEndDate != null)
                            && phase.Status == PhaseStatus.NOT_STARTED
                        )
                        {
                            if (request.EstimatedStartDate >= request.EstimatedEndDate)
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = "Ngày bắt đầu phải trước ngày kết thúc";
                                return commonResponse;
                            }

                            // schdule
                            if (request.EstimatedStartDate != null)
                            {
                                phase.EstimatedStartDate = request.EstimatedStartDate.Value;
                                TimeSpan startDelay = phase.EstimatedStartDate - DateTime.Now;
                                if (startDelay.TotalMilliseconds > 0)
                                {
                                    var jobId = BackgroundJob.Schedule(
                                        () => StartPhase(phase.Id),
                                        startDelay
                                    );
                                }
                            }
                            if (request.EstimatedEndDate != null)
                            {
                                phase.EstimatedEndDate = request.EstimatedEndDate.Value;
                                TimeSpan endDelay = phase.EstimatedEndDate - DateTime.Now;
                                if (endDelay.TotalMilliseconds > 0)
                                {
                                    var jobId = BackgroundJob.Schedule(
                                        () => EndPhase(phase.Id),
                                        endDelay
                                    );
                                }
                            }
                        }
                        else if (
                            (request.EstimatedStartDate != null || request.EstimatedEndDate != null)
                            && phase.Status != PhaseStatus.NOT_STARTED
                        )
                        {
                            commonResponse.Status = 400;
                            commonResponse.Message =
                                "Bạn chỉ có thể cập nhật thời gian bắt đầu và kết thúc khi ở giai đoạn chờ";
                            return commonResponse;
                        }
                        if (request.Name != null)
                        {
                            phase.Name = request.Name;
                        }

                        if (request.status == 0 && phase.Status == PhaseStatus.NOT_STARTED)
                        {
                            phase.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
                            phase.Status = PhaseStatus.STARTED;
                        }
                        else if (request.status == 1 && phase.Status == PhaseStatus.STARTED)
                        {
                            phase.EndDate = SettedUpDateTime.GetCurrentVietNamTime();
                            phase.Status = PhaseStatus.ENDED;
                        }

                        await _phaseRepository.UpdatePhaseAsync(phase);
                        commonResponse.Status = 200;
                        commonResponse.Message = "Cập nhật thành công";
                    }
                    else
                    {
                        commonResponse.Message = "Không tìm thấy phase này";
                        commonResponse.Status = 200;
                        return commonResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(CreatePharse);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> DeletePharse(Guid phaseId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Phase? phase = await _phaseRepository.GetPhaseByIdAsync(phaseId);
                if (phase != null)
                {
                    if (phase.Status != PhaseStatus.NOT_STARTED)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = "Bạn không thể xóa các phase đã diễn ra";
                        return commonResponse;
                    }

                    await _phaseRepository.DeletePhaseAsync(phase);
                    commonResponse.Status = 200;
                    commonResponse.Message = "Xóa thành công";
                }
                else
                {
                    commonResponse.Message = "Không tìm thấy phase này";
                    commonResponse.Status = 200;
                    return commonResponse;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(DeletePharse);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        //public async Task<CommonResponse> StartPhase(Guid phaseId)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        Phase? phase = await _phaseRepository.GetPhaseByIdAsync(phaseId);
        //        if (
        //            phase != null
        //            && phase.Status == BusinessObject.EntityEnums.PhaseStatus.NOT_STARTED
        //        )
        //        {
        //            phase.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
        //            phase.Status = BusinessObject.EntityEnums.PhaseStatus.STARTED;
        //            await _phaseRepository.UpdatePhaseAsync(phase);
        //            commonResponse.Status = 200;
        //            commonResponse.Message = "Cập nhật thành công";
        //        }
        //        else
        //        {
        //            commonResponse.Message = "Không tìm thấy phase này";
        //            commonResponse.Status = 200;
        //            return commonResponse;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string className = nameof(PhaseService);
        //        string methodName = nameof(StartPhase);
        //        _logger.LogError(
        //            ex,
        //            "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
        //            className,
        //            methodName,
        //            ex.Message
        //        );
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //    }
        //    return commonResponse;
        //}
        public void StartPhase(Guid phaseId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Phase? phase = _phaseRepository.GetPhaseById(phaseId);
                if (phase != null && phase.Status == PhaseStatus.NOT_STARTED)
                {
                    phase.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
                    phase.Status = PhaseStatus.STARTED;
                    _phaseRepository.UpdatePhase(phase);
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(StartPhase);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
            }
        }

        public void EndPhase(Guid phaseId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Phase? phase = _phaseRepository.GetPhaseById(phaseId);
                if (phase != null && phase.Status == PhaseStatus.STARTED)
                {
                    phase.EndDate = SettedUpDateTime.GetCurrentVietNamTime();
                    phase.Status = PhaseStatus.ENDED;
                    _phaseRepository.UpdatePhase(phase);
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(StartPhase);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
            }
        }

        //public async Task<CommonResponse> EndPhase(Guid phaseId)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        Phase? phase = await _phaseRepository.GetPhaseByIdAsync(phaseId);
        //        if (phase != null && phase.Status == BusinessObject.EntityEnums.PhaseStatus.ENDED)
        //        {
        //            phase.StartDate = SettedUpDateTime.GetCurrentVietNamTime();
        //            phase.Status = BusinessObject.EntityEnums.PhaseStatus.STARTED;
        //            await _phaseRepository.UpdatePhaseAsync(phase);
        //            commonResponse.Status = 200;
        //            commonResponse.Message = "Cập nhật thành công";
        //        }
        //        else
        //        {
        //            commonResponse.Message = "Không tìm thấy phase này";
        //            commonResponse.Status = 200;
        //            return commonResponse;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string className = nameof(PhaseService);
        //        string methodName = nameof(CreatePharse);
        //        _logger.LogError(
        //            ex,
        //            "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
        //            className,
        //            methodName,
        //            ex.Message
        //        );
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //    }
        //    return commonResponse;
        //}

        public async Task<CommonResponse> GetPhaseByActivityId(Guid activityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                var data = await _phaseRepository.GetPhaseByActivityIdAsync(activityId);
                if (data != null && data.Count > 0)
                {
                    var result = data.Select(
                            p =>
                                new
                                {
                                    p.Id,
                                    p.Name,
                                    p.StartDate,
                                    p.EndDate,
                                    p.EstimatedStartDate,
                                    p.EstimatedEndDate,
                                    Status = p.Status.ToString(),
                                    p.ActivityId,
                                    order = p.Order
                                }
                        )
                        .OrderBy(p => p.order)
                        .ToList();
                    commonResponse.Data = result;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(PhaseService);
                string methodName = nameof(CreatePharse);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }
    }
}
