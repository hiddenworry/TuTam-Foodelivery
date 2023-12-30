namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class ActivityFeedbackServiceTests
    {
        //private Mock<IActivityFeedbackRepository> _mockActivityFeedbackRepository;
        //private Mock<IConfiguration> _mockConfig;
        //private Mock<ILogger<ActivityFeedbackService>> _mockLogger;
        //private Mock<IActivityMemberRepository> _mockActivityMemberRepository;
        //private Mock<IUserRepository> _mockUserRepository;
        //private ActivityFeedbackService _service;

        //[SetUp]
        //public void Setup()
        //{
        //    _mockActivityFeedbackRepository = new Mock<IActivityFeedbackRepository>();
        //    _mockConfig = new Mock<IConfiguration>();
        //    _mockLogger = new Mock<ILogger<ActivityFeedbackService>>();
        //    _mockActivityMemberRepository = new Mock<IActivityMemberRepository>();
        //    _mockUserRepository = new Mock<IUserRepository>();

        //    _service = new ActivityFeedbackService(
        //        _mockActivityFeedbackRepository.Object,
        //        _mockConfig.Object,
        //        _mockUserRepository.Object,
        //        _mockLogger.Object,
        //        _mockActivityMemberRepository.Object
        //    );
        //}

        //[Test]
        //public async Task SendFeedback_ShouldUpdateSuccessfully_WhenFeedbackExists()
        //{
        //    // Arrange
        //    var fakeFeedback = new ActivityFeedback
        //    {
        //        // Set properties as needed for the test
        //    };

        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.FindActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<Guid>(),
        //                    BusinessObject.EntityEnums.ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
        //                )
        //        )
        //        .ReturnsAsync(fakeFeedback);

        //    _mockActivityFeedbackRepository
        //        .Setup(repo => repo.UpdateFeedbackAsync(It.IsAny<ActivityFeedback>()))
        //        .ReturnsAsync(1); // Indicate success

        //    var request = new ActivityFeedbackCreatingRequest
        //    {
        //        // Populate request properties as needed
        //    };

        //    // Act
        //    var result = await _service.SendFeedback(Guid.NewGuid(), request);

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.AreEqual("Đã cập nhật thành công.", result.Message);
        //}

        //[Test]
        //public async Task SendFeedback_ShouldReturnBadRequest_WhenFeedbackNotFound()
        //{
        //    // Arrange
        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.FindActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<Guid>(),
        //                    BusinessObject.EntityEnums.ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
        //                )
        //        )
        //        .ReturnsAsync((ActivityFeedback)null); // No feedback found

        //    var request = new ActivityFeedbackCreatingRequest
        //    {
        //        // Populate request properties
        //    };

        //    // Act
        //    var result = await _service.SendFeedback(Guid.NewGuid(), request);

        //    // Assert
        //    Assert.AreEqual(400, result.Status);
        //    Assert.AreEqual("Bạn không thể đưa ra đánh giá với hoạt động này.", result.Message);
        //}

        //[Test]
        //public async Task CreateFeedback_ShouldCreateFeedbackForAllActiveMembers_WhenMembersExist()
        //{
        //    // Arrange
        //    var fakeActivityMembers = new List<ActivityMember>
        //    { /* populate with test members */
        //    };
        //    _mockActivityMemberRepository
        //        .Setup(
        //            repo =>
        //                repo.FindMemberByActivityIdAsync(
        //                    It.IsAny<Guid>(),
        //                    BusinessObject.EntityEnums.ActivityMemberStatus.ACTIVE
        //                )
        //        )
        //        .ReturnsAsync(fakeActivityMembers);

        //    _mockActivityFeedbackRepository
        //        .Setup(repo => repo.CreateFeedbackAsync(It.IsAny<ActivityFeedback>()))
        //        .ReturnsAsync(1); // Simulate successful creation

        //    // Act
        //    var result = await _service.CreatFeedback(Guid.NewGuid(),);

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.AreEqual("Đã cập nhật thành công.", result.Message);
        //    _mockActivityFeedbackRepository.Verify(
        //        repo => repo.CreateFeedbackAsync(It.IsAny<ActivityFeedback>()),
        //        Times.Exactly(fakeActivityMembers.Count)
        //    );
        //}

        //[Test]
        //public async Task CreateFeedback_ShouldReturnSuccess_WhenNoActiveMembers()
        //{
        //    // Arrange
        //    _mockActivityMemberRepository
        //        .Setup(
        //            repo =>
        //                repo.FindMemberByActivityIdAsync(
        //                    It.IsAny<Guid>(),
        //                    BusinessObject.EntityEnums.ActivityMemberStatus.ACTIVE
        //                )
        //        )
        //        .ReturnsAsync(new List<ActivityMember>());

        //    // Act
        //    var result = await _service.CreatFeedback(Guid.NewGuid());

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.AreEqual("Đã cập nhật thành công.", result.Message);
        //    _mockActivityFeedbackRepository.Verify(
        //        repo => repo.CreateFeedbackAsync(It.IsAny<ActivityFeedback>()),
        //        Times.Never
        //    );
        //}

        //[Test]
        //public async Task CreateFeedback_ShouldReturnInternalServerError_WhenExceptionOccurs()
        //{
        //    // Arrange
        //    _mockActivityMemberRepository
        //        .Setup(
        //            repo =>
        //                repo.FindMemberByActivityIdAsync(
        //                    It.IsAny<Guid>(),
        //                    BusinessObject.EntityEnums.ActivityMemberStatus.ACTIVE
        //                )
        //        )
        //        .ThrowsAsync(new Exception("Test exception"));

        //    // Act
        //    var result = await _service.CreatFeedback(Guid.NewGuid());

        //    // Assert
        //    Assert.AreEqual(500, result.Status);
        //}

        //[Test]
        //public async Task CheckUserIsFeedbacked_ShouldReturnFeedback_WhenFeedbackExists()
        //{
        //    // Arrange
        //    var fakeFeedback = new ActivityFeedback
        //    {
        //        // Populate with properties
        //    };

        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.FindActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<Guid>(),
        //                    ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
        //                )
        //        )
        //        .ReturnsAsync(fakeFeedback);

        //    // Act
        //    var result = await _service.CheckUserIsFeedbacked(Guid.NewGuid(), Guid.NewGuid());

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.IsNotNull(result.Data);
        //    // Additional assertions to verify the data content
        //}

        //[Test]
        //public async Task CheckUserIsFeedbacked_ShouldReturnNull_WhenNoFeedbackFound()
        //{
        //    // Arrange
        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.FindActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<Guid>(),
        //                    ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
        //                )
        //        )
        //        .ReturnsAsync((ActivityFeedback)null);

        //    // Act
        //    var result = await _service.CheckUserIsFeedbacked(Guid.NewGuid(), Guid.NewGuid());

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.IsNull(result.Data);
        //}

        //[Test]
        //public async Task CheckUserIsFeedbacked_ShouldReturnInternalServerError_WhenExceptionOccurs()
        //{
        //    // Arrange
        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.FindActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<Guid>(),
        //                    ActivityFeedbackStatus.NOT_FEEDBACK_PROVIDED
        //                )
        //        )
        //        .ThrowsAsync(new Exception("Test exception"));

        //    // Act
        //    var result = await _service.CheckUserIsFeedbacked(Guid.NewGuid(), Guid.NewGuid());

        //    // Assert
        //    Assert.AreEqual(500, result.Status);
        //}

        //[Test]
        //public async Task GetFeedbackForAdmin_ShouldReturnFeedbacksWithPagination_WhenFeedbackExists()
        //{
        //    // Arrange
        //    var fakeFeedbacks = new List<ActivityFeedback>
        //    {
        //        new ActivityFeedback
        //        {
        //            Activity = new Activity { Id = new Guid(), Name = "activity" },
        //            CreatedDate = DateTime.Now,
        //            Id = new Guid(),
        //            Rating = 5.0,
        //            Content = "ABCD",
        //            Status = ActivityFeedbackStatus.FEEDBACK_PROVIDED,
        //            UserId = new Guid()
        //        }
        //    };
        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.GetListActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<ActivityFeedbackStatus?>()
        //                )
        //        )
        //        .ReturnsAsync(fakeFeedbacks);

        //    int? page = 1;
        //    int? pageSize = 10;

        //    // Act
        //    var result = await _service.GetFeedbackForAdmin(page, pageSize, Guid.NewGuid(), null);

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.IsNotNull(result.Pagination);
        //    // Additional assertions to verify the data content and pagination details
        //}

        //[Test]
        //public async Task GetFeedbackForAdmin_ShouldReturnInternalServerError_WhenExceptionOccurs()
        //{
        //    // Arrange
        //    _mockActivityFeedbackRepository
        //        .Setup(
        //            repo =>
        //                repo.GetListActivityFeedbackAsync(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<ActivityFeedbackStatus?>()
        //                )
        //        )
        //        .ThrowsAsync(new Exception("Test exception"));

        //    // Act
        //    var result = await _service.GetFeedbackForAdmin(null, null, Guid.NewGuid(), null);

        //    // Assert
        //    Assert.AreEqual(500, result.Status);
        //}
    }
}
