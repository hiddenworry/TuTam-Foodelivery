using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class PostServiceTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock;
        private readonly Mock<IFirebaseStorageService> _firebaseStorageServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IHubContext<NotificationSignalSender>> _hubContextMock;
        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            _postRepositoryMock = new Mock<IPostRepository>();
            _firebaseStorageServiceMock = new Mock<IFirebaseStorageService>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _configMock = new Mock<IConfiguration>();
            _hubContextMock = new Mock<IHubContext<NotificationSignalSender>>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            // Setup mock configuration values as needed


            _postService = new PostService(
                _postRepositoryMock.Object,
                _firebaseStorageServiceMock.Object,
                _loggerMock.Object,
                _configMock.Object,
                _hubContextMock.Object,
                _notificationRepositoryMock.Object,
                _userRepositoryMock.Object
            );
        }

        [Test]
        public async Task CreatePost_WhenUserNotFound_ReturnsUserNotFoundResponse()
        {
            // Arrange
            var postRequest = new PostCreatingRequest
            {
                // Populate with test data
            };
            Guid testUserId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(testUserId))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _postService.CreatePost(postRequest, testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Status, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("không tìm thấy người dùng này."));
        }

        [Test]
        public async Task CreatePost_WhenRegularUser_CreatesUnverifiedPost()
        {
            // Arrange
            var postRequest = new PostCreatingRequest
            {
                Content = "abcd",
                Images = new List<Microsoft.AspNetCore.Http.IFormFile>()
            };
            Guid testUserId = Guid.NewGuid();
            var mockUser = new User { Role = new Role { Name = RoleEnum.CONTRIBUTOR.ToString() } };

            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(testUserId))
                .ReturnsAsync(mockUser);
            _postRepositoryMock.Setup(repo => repo.CreatePostAsync(It.IsAny<Post>())); // You can add more specific setup if needed

            // Act
            var result = await _postService.CreatePost(postRequest, testUserId);

            // Assert
            Assert.NotNull(result);

            Assert.That(result.Status, Is.EqualTo(200));
            _postRepositoryMock.Verify(
                repo => repo.CreatePostAsync(It.Is<Post>(p => p.Status == PostStatus.UNVERIFIED)),
                Times.Once
            );
        }

        [Test]
        public async Task CreatePost_ForSystemAdmin_CreateActivePost()
        {
            // Arrange
            var postRequest = new PostCreatingRequest
            {
                Content = "abcd",
                Images = new List<Microsoft.AspNetCore.Http.IFormFile>()
            };
            Guid testUserId = Guid.NewGuid();
            var mockUser = new User { Role = new Role { Name = RoleEnum.SYSTEM_ADMIN.ToString() } };

            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(testUserId))
                .ReturnsAsync(mockUser);
            _postRepositoryMock.Setup(repo => repo.CreatePostAsync(It.IsAny<Post>())); // You can add more specific setup if needed

            // Act
            var result = await _postService.CreatePost(postRequest, testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Status, Is.EqualTo(200));
            _postRepositoryMock.Verify(
                repo => repo.CreatePostAsync(It.Is<Post>(p => p.Status == PostStatus.ACTIVE)),
                Times.Once
            );
        }

        [Test]
        public async Task GetPostForUser_ReturnsActivePosts_WithValidPagination()
        {
            var fakeUser = new User
            {
                Avatar = "link-to-avatar",
                Name = "John Doe",
                Phone = "1234567890",
                Email = "johndoe@example.com",
                Id = Guid.NewGuid(),
                Role = new Role { DisplayName = RoleEnum.CHARITY.ToString() },
                Status = UserStatus.ACTIVE // hoặc trạng thái khác tùy thuộc vào định nghĩa của bạn
            };
            List<Post>? fakePosts = new List<Post>()
            {
                new Post
                {
                    Content = "Sample content",
                    Id = Guid.NewGuid(),
                    Images = "image1.jpg,image2.jpg",
                    CreatedDate = DateTime.Now,
                    Status = PostStatus.ACTIVE, // Hoặc trạng thái tương ứng
                    CreaterId = fakeUser.Id
                }
            };

            _postRepositoryMock
                .Setup(repo => repo.GetPosts(PostStatus.ACTIVE, null))
                .ReturnsAsync(fakePosts);
            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fakeUser);
            var result = await _postService.GetPostForUser(null, null);
            // Kiểm tra kết quả
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.NotNull(result.Data);
        }

        [Test]
        public async Task ConfirmPost_WithNonexistentPost_ReturnsErrorMessage()
        {
            // Thiết lập mô phỏng để không tìm thấy bài viết
            _postRepositoryMock
                .Setup(repo => repo.GetPostById(It.IsAny<Guid>()))
                .ReturnsAsync((Post)null!);

            // Tạo đối tượng PostService với các phụ thuộc giả


            // Gọi hàm ConfirmPost
            var result = await _postService.ConfirmPost(Guid.NewGuid(), new ConfirmPostRequest());

            // Kiểm tra kết quả
            Assert.That(result.Status, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Không tìm thấy bài post."));
        }

        //[Test]
        //public async Task ConfirmPost_WhenPostIsAccepted_ReturnsStatus200()
        //{
        //    _configMock
        //        .Setup(c => c["ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"])
        //        .Returns("Internal Server Error");
        //    _configMock.Setup(c => c["Notification:Image"]).Returns("notification-image-url");

        //    var fakePost = new Post
        //    {
        //        Id = Guid.NewGuid(),
        //        Status = PostStatus.UNVERIFIED,
        //        CreatedBy = Guid.NewGuid() // Giả định ID người tạo
        //    };

        //    _postRepositoryMock
        //        .Setup(repo => repo.GetPostById(It.IsAny<Guid>()))
        //        .ReturnsAsync(fakePost);
        //    var mockHubContext = new Mock<IHubContext<NotificationSignalSender>>();
        //    var mockClients = new Mock<IHubClients>();
        //    var mockClientProxy = new Mock<IClientProxy>();

        //    mockHubContext.Setup(hub => hub.Clients).Returns(mockClients.Object);
        //    mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);

        //    mockClientProxy
        //        .Setup(
        //            proxy =>
        //                proxy.SendAsync(
        //                    It.IsAny<string>(),
        //                    It.IsAny<object>(),
        //                    It.IsAny<CancellationToken>()
        //                )
        //        )
        //        .Returns(Task.CompletedTask);
        //    var confirmPostRequest = new ConfirmPostRequest { isAccept = true };
        //    var result = await _postService.ConfirmPost(fakePost.Id, confirmPostRequest);
        //    Assert.That(result.Status, Is.EqualTo(200));
        //    Assert.That(result.Message, Is.EqualTo("Cập nhật thành công"));
        //    _postRepositoryMock.Verify(
        //        repo => repo.UpdatePostAsync(It.Is<Post>(p => p.Status == PostStatus.ACTIVE)),
        //        Times.Once
        //    );
        //    _notificationRepositoryMock.Verify(
        //        repo => repo.CreateNotificationAsync(It.IsAny<Notification>()),
        //        Times.Once
        //    );
        //}
    }
}
