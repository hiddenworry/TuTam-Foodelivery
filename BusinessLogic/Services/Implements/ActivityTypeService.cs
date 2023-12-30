using DataAccess.Entities;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services.Implements
{
    public class ActivityTypeService : IActivityTypeService
    {
        private readonly IActivityTypeRepository _activityTypeRepository;
        private readonly IConfiguration _config;

        public ActivityTypeService(
            IActivityTypeRepository activityTypeRepository,
            IConfiguration config
        )
        {
            _activityTypeRepository = activityTypeRepository;
            _config = config;
        }

        public async Task<CommonResponse> GetAllActivityTypesAsync()
        {
            List<ActivityType> activityTypes =
                await _activityTypeRepository.GetAllActivityTypesAsync();
            List<ActivityTypeResponse> activityTypeResponses = activityTypes
                .Select(a => new ActivityTypeResponse { Id = a.Id, Name = a.Name })
                .ToList();
            return new CommonResponse
            {
                Status = 200,
                Data = activityTypeResponses,
                Message = _config["ResponseMessages:ActivityTypeMsg:GetActivityTypesSuccessMsg"]
            };
        }
    }
}
