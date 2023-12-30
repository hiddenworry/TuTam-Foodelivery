using BusinessLogic.Services.Implements;
using DataAccess.Entities;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class PostCommentServiceTests
    {
        private Mock<IPostCommentRepository> _mockPostCommentRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<ILogger<PostCommentService>> _mockLogger;
        private Mock<IConfiguration> _mockConfig;
        private PostCommentService _service;

        [SetUp]
        public void Setup()
        {
            _mockPostCommentRepository = new Mock<IPostCommentRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<PostCommentService>>();
            _mockConfig = new Mock<IConfiguration>();

            _service = new PostCommentService(
                _mockPostCommentRepository.Object,
                _mockUserRepository.Object,
                _mockLogger.Object,
                _mockConfig.Object
            );
        }

        [Test]
        public async Task AddComment_Return_Status_200()
        {
            // Arrange
            var fakeComment = new PostComment
            { /* Set properties */
            };
            CommentCreatingRequest commentCreatingRequest = new CommentCreatingRequest
            {
                Content = "abcdef",
                PostId = new Guid()
            };
            _mockPostCommentRepository
                .Setup(x => x.CreateCommnentAsync(It.IsAny<PostComment>()))
                .ReturnsAsync(1); // Assuming that '1' is the expected return value on success

            _mockUserRepository
                .Setup(x => x.FindUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new User());
            Guid userId = new Guid();

            CommonResponse result = await _service.CreateComment(commentCreatingRequest, userId);

            // Assert

            Assert.That(result.Status, Is.EqualTo(200));
        }

        [Test]
        public async Task AddComment_Return_Status_400()
        {
            // Arrange
            var fakeComment = new PostComment
            { /* Set properties */
            };
            CommentCreatingRequest commentCreatingRequest = new CommentCreatingRequest
            {
                Content = "abcdef",
                PostId = new Guid()
            };
            _mockPostCommentRepository
                .Setup(x => x.CreateCommnentAsync(It.IsAny<PostComment>()))
                .ReturnsAsync(1); // Assuming that '1' is the expected return value on success
            Guid userId = new Guid();

            CommonResponse result = await _service.CreateComment(commentCreatingRequest, userId);

            Assert.That(result.Status, Is.EqualTo(400));
        }
    }
}
