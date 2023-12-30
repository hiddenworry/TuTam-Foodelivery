using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.OpenRouteService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class ActivityServiceTests
    {
        private Mock<IActivityRepository> _mockActivityRepository;
        private Mock<IBranchRepository> _mockBranchRepository;
        private Mock<IActivityTypeComponentRepository> _mockActivityTypeComponentRepository;
        private Mock<IActivityBranchRepository> _mockActivityBranchRepository;
        private Mock<IConfiguration> _mockConfig;
        private Mock<IActivityTypeRepository> _mockActivityTypeRepository;
        private Mock<IFirebaseStorageService> _mockFirebaseStorageService;
        private Mock<ILogger<ActivityService>> _mockLogger;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<ICharityUnitRepository> _mockCharityUnitRepository;
        private Mock<ITargetProcessRepository> _mockTargetProcessRepository;
        private Mock<IItemRepository> _mockItemRepository;
        private Mock<IActivityMemberRepository> _mockActivityMemberRepository;
        private Mock<IRoleRepository> _mockRoleRepository;
        private Mock<IAidItemRepository> _mockAidItemRepository;
        private Mock<IOpenRouteService> _mockOpenRouteService;

        private ActivityService _activityService;

        [SetUp]
        public void Setup()
        {
            _mockActivityRepository = new Mock<IActivityRepository>();
            _mockBranchRepository = new Mock<IBranchRepository>();
            _mockActivityTypeComponentRepository = new Mock<IActivityTypeComponentRepository>();
            _mockActivityBranchRepository = new Mock<IActivityBranchRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockActivityTypeRepository = new Mock<IActivityTypeRepository>();
            _mockFirebaseStorageService = new Mock<IFirebaseStorageService>();
            _mockLogger = new Mock<ILogger<ActivityService>>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCharityUnitRepository = new Mock<ICharityUnitRepository>();
            _mockTargetProcessRepository = new Mock<ITargetProcessRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockActivityMemberRepository = new Mock<IActivityMemberRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockAidItemRepository = new Mock<IAidItemRepository>();
            _mockOpenRouteService = new Mock<IOpenRouteService>();

            _activityService = new ActivityService(
                _mockActivityRepository.Object,
                _mockBranchRepository.Object,
                _mockActivityTypeComponentRepository.Object,
                _mockActivityBranchRepository.Object,
                _mockConfig.Object,
                _mockActivityTypeRepository.Object,
                _mockFirebaseStorageService.Object,
                _mockLogger.Object,
                _mockUserRepository.Object,
                _mockCharityUnitRepository.Object,
                _mockTargetProcessRepository.Object,
                _mockItemRepository.Object,
                _mockActivityMemberRepository.Object,
                _mockRoleRepository.Object,
                _mockAidItemRepository.Object,
                _mockOpenRouteService.Object
            );
        }

        //[Test]
        //public async Task GetActivitiesAsync_WithParameters_ShouldReturnFilteredActivities()
        //{
        //    // Arrange - setting up test data
        //    int page = 1;
        //    int pageSize = 10;
        //    ActivityStatus status = ActivityStatus.STARTED; // Assuming an enum for status
        //    Guid activityTypeId = new Guid("F312B640-2751-EE11-9F1B-C809A8BFD17D");
        //    DateTime startDate = new DateTime(2023, 11, 30, 1, 0, 0);
        //    DateTime endDate = new DateTime(2023, 2, 30, 1, 0, 0); // Note: Feb 30 is an invalid date
        //    bool isJoined = true;
        //    Guid userId = new Guid("A312B640-2751-EE11-9F1B-C809A8BFD17D");
        //    string address =
        //        "Trường Đại học FPT TP. HCM, Đường D1, Long Thạnh Mỹ, Thành Phố Thủ Đức, Thành phố Hồ Chí Minh";

        //    var expectedActivities = new List<Activity>
        //    { /* Populate with mock activities */
        //    };
        //    _mockActivityRepository
        //        .Setup(
        //            repo =>
        //                repo.GetActivitiesAsync(
        //                    It.IsAny<string>(),
        //                    It.IsAny<ActivityStatus?>(),
        //                    It.IsAny<ActivityScope?>(),
        //                    It.IsAny<List<Guid>?>(),
        //                    It.IsAny<DateTime?>(),
        //                    It.IsAny<DateTime?>(),
        //                    It.IsAny<Guid?>(),
        //                    It.IsAny<Guid?>(),
        //                    It.IsAny<string>(),
        //                    It.IsAny<string>()
        //                )
        //        )
        //        .ReturnsAsync(expectedActivities);

        //    // Act


        //    // Assert
        //}
    }
}
