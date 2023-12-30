using BusinessLogic.Services.Implements;
using DataAccess.Repositories;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class PhaseServiceTests
    {
        private Mock<IPhaseRepository> _mockPhaseRepository;
        private Mock<ILogger<PhaseService>> _mockLogger;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IActivityRepository> _mockActivityRepository;
        private PhaseService _service;

        [SetUp]
        public void Setup()
        {
            _mockPhaseRepository = new Mock<IPhaseRepository>();
            _mockLogger = new Mock<ILogger<PhaseService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockActivityRepository = new Mock<IActivityRepository>();

            _service = new PhaseService(
                _mockPhaseRepository.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockActivityRepository.Object
            );
        }

        //[Test]
        //public async Task CreatePhase_ActivityNotFound_ShouldReturnBadRequest()
        //{
        //    // Arrange
        //    _mockActivityRepository
        //        .Setup(repo => repo.FindActivityByIdAsync(It.IsAny<Guid>()))
        //        .ReturnsAsync((BusinessObject.Entities.Activity)null);

        //    var request = new PharseCreatingRequest
        //    {
        //        ActivityId = Guid.NewGuid(), // A new unique identifier
        //        phaseRequests = new List<PhaseRequest>()
        //        {
        //            new PhaseRequest
        //            {
        //                EstimatedStartDate = DateTime.Now.AddDays(1),
        //                EstimatedEndDate = DateTime.Now.AddDays(2),
        //                Name = "Phase 1"
        //            },
        //        }
        //    };
        //    var mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        //    mockBackgroundJobClient
        //        .Setup(
        //            client =>
        //                client.Schedule<PhaseService>(
        //                    It.IsAny<Expression<Action<PhaseService>>>(),
        //                    It.IsAny<TimeSpan>()
        //                )
        //        )
        //        .Returns("mock-job-id");

        //    var result = await _service.CreatePharse(request);
        //    Assert.AreEqual(400, result.Status);
        //}

        //[Test]
        //public async Task CreatePhase_WithValidRequest_ShouldScheduleJobs()
        //{
        //    var mockBackgroundJobScheduler = new Mock<IBackgroundJobScheduler>();
        //    var mockActivity = new BusinessObject.Entities.Activity
        //    { /* Populate necessary properties */
        //    };
        //    _mockActivityRepository
        //        .Setup(repo => repo.FindActivityByIdAsync(It.IsAny<Guid>()))
        //        .ReturnsAsync(mockActivity);

        //    var startDate = DateTime.Now.AddDays(1);
        //    var endDate = DateTime.Now.AddDays(2);

        //    var request = new PharseCreatingRequest
        //    {
        //        ActivityId = Guid.NewGuid(),
        //        phaseRequests = new List<PhaseRequest>()
        //        {
        //            new PhaseRequest
        //            {
        //                EstimatedStartDate = startDate,
        //                EstimatedEndDate = endDate,
        //                Name = "Phase 1"
        //            }
        //        }
        //    };

        //    // Act
        //    var result = await _service.CreatePharse(request);

        //    // Assert
        //    Assert.AreEqual(200, result.Status);
        //    Assert.AreEqual("Tạo thành công", result.Message);

        //    // Verify that ScheduleJob is called
        //    mockBackgroundJobScheduler.Verify(
        //        x =>
        //            x.ScheduleJob<PhaseService>(
        //                It.IsAny<Expression<Action<PhaseService>>>(),
        //                It.Is<TimeSpan>(ts => ts > TimeSpan.Zero)
        //            ),
        //        Times.AtLeastOnce()
        //    );
        // }
    }

    public interface IBackgroundJobScheduler
    {
        string ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    }

    public class BackgroundJobScheduler : IBackgroundJobScheduler
    {
        public string ScheduleJob<T>(Expression<Action<T>> methodCall, TimeSpan delay)
        {
            return BackgroundJob.Schedule(methodCall, delay);
        }
    }
}
