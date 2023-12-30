using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.Notification.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _config;
        private readonly IHubContext<NotificationSignalSender> _hubContext;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;

        public PostService(
            IPostRepository postRepository,
            IFirebaseStorageService firebaseStorageService,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IHubContext<NotificationSignalSender> hubContext,
            INotificationRepository notificationRepository,
            IUserRepository userRepository
        )
        {
            _postRepository = postRepository;
            _firebaseStorageService = firebaseStorageService;
            _logger = logger;
            _config = configuration;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<CommonResponse> CreatePost(
            PostCreatingRequest postCreatingRequest,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);

                Post post = new Post();
                post.Status = PostStatus.UNVERIFIED;
                if (user == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "không tìm thấy người dùng này.";
                    return commonResponse;
                }
                else if (
                    user.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString()
                    || user.Role.Name == RoleEnum.BRANCH_ADMIN.ToString()
                )
                {
                    post.Status = PostStatus.ACTIVE;
                }
                post.Content = postCreatingRequest.Content;

                post.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                List<string> imageUrls = new List<string>();
                foreach (var a in postCreatingRequest.Images)
                {
                    using (var stream = a.OpenReadStream())
                    {
                        string imageName =
                            Guid.NewGuid().ToString() + Path.GetExtension(a.FileName);
                        string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                            stream,
                            imageName
                        );
                        imageUrls.Add(imageUrl);
                    }
                }
                post.Images = string.Join(",", imageUrls);

                post.CreaterId = userId;
                await _postRepository.CreatePostAsync(post);
                commonResponse.Status = 200;
                commonResponse.Message = "Đã gửi yêu cầu";
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreatePost);
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

        public async Task<CommonResponse> GetPostForUser(int? page, int? pageSize)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<Post>? listPost = await _postRepository.GetPosts(PostStatus.ACTIVE, null);

                if (listPost != null && listPost.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = listPost.Count;
                    listPost = listPost
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();

                    List<object> rs = new List<object>();
                    foreach (var p in listPost)
                    {
                        User? tmp = await _userRepository.FindUserByIdAsync(p.CreaterId);
                        SimpleUserResponse? simpleUser = null;
                        if (tmp != null)
                        {
                            simpleUser = new SimpleUserResponse
                            {
                                Avatar = tmp.Avatar,
                                FullName = tmp.Name!,
                                Phone = tmp.Phone,
                                Email = tmp.Email,
                                Id = tmp.Id,
                                Role = tmp.Role.DisplayName,
                                Status = tmp.Status.ToString()
                            };
                        }
                        object a = new
                        {
                            p.Content,
                            p.Id,
                            Images = p.Images.Split(",").ToList(),
                            p.CreatedDate,
                            Status = p.Status.ToString(),
                            CreateBy = simpleUser,
                            PostComments = p.PostComments == null
                                ? null
                                : p.PostComments
                                    .Select(
                                        pm =>
                                            new
                                            {
                                                pm.Id,
                                                pm.PostId,
                                                pm.CreatedDate,
                                                pm.UserId,
                                                pm.Status,
                                                UserFullName = pm.User?.Name
                                            }
                                    )
                                    .ToList()
                        };
                        rs.Add(a);
                    }
                    commonResponse.Data = rs;
                    commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreatePost);
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

        public async Task<CommonResponse> GetPostByStatusAndUserId(
            int? page,
            int? pageSize,
            PostStatus postStatus,
            Guid userId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);
                List<Post>? listPost = new List<Post>();
                if (user != null)
                {
                    if (user.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString())
                    {
                        listPost = await _postRepository.GetPosts(postStatus, null);
                    }
                    else
                    {
                        listPost = await _postRepository.GetPosts(postStatus, userId);
                    }
                    if (listPost != null && listPost.Count > 0)
                    {
                        Pagination pagination = new Pagination();
                        pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                        pagination.CurrentPage = page == null ? 1 : page.Value;
                        pagination.Total = listPost.Count;
                        listPost = listPost
                            .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                            .Take(pagination.PageSize)
                            .ToList();

                        List<object> rs = new List<object>();
                        foreach (var p in listPost)
                        {
                            User? tmp = await _userRepository.FindUserByIdAsync(p.CreaterId);
                            SimpleUserResponse? simpleUser = null;
                            if (tmp != null)
                            {
                                simpleUser = new SimpleUserResponse
                                {
                                    Avatar = tmp.Avatar,
                                    FullName = tmp.Name!,
                                    Phone = tmp.Phone,
                                    Email = tmp.Email,
                                    Id = tmp.Id,
                                    Role = tmp.Role.DisplayName,
                                    Status = tmp.Status.ToString()
                                };
                            }
                            object a = new
                            {
                                p.Content,
                                p.Id,
                                Images = p.Images.Split(",").ToList(),
                                p.CreatedDate,
                                Status = p.Status.ToString(),
                                CreateBy = simpleUser,
                                PostComments = p.PostComments.Select(
                                    pm =>
                                        new
                                        {
                                            pm.Id,
                                            pm.PostId,
                                            pm.CreatedDate,
                                            pm.UserId,
                                            pm.Status,
                                            UserFullName = pm.User.Name
                                        }
                                )
                            };
                            rs.Add(a);
                        }
                        commonResponse.Data = rs;
                        commonResponse.Pagination = pagination;
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Bạn không có quyền để thực hiện hành động này";
                    return commonResponse;
                }

                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreatePost);
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

        public async Task<CommonResponse> ConfirmPost(Guid postId, ConfirmPostRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            string notificationImage = _config["Notification:Image"];
            try
            {
                Post? post = await _postRepository.GetPostById(postId);
                if (post != null && post.Status == PostStatus.UNVERIFIED)
                {
                    if (request.isAccept)
                    {
                        post.Status = PostStatus.ACTIVE;
                        await _postRepository.UpdatePostAsync(post);
                        Notification notification = new Notification
                        {
                            Name = "Bài viết của bạn đã được sự chấp nhận trên nền tảng.",
                            Content = "Bài viết của bạn đã được sự chấp nhận trên nền tảng.",
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            ReceiverId = post.CreaterId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            Image = notificationImage,
                            DataType = DataNotificationType.POST,
                            DataId = post.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            post.CreaterId.ToString(),
                            notification
                        );
                        commonResponse.Status = 200;
                        commonResponse.Message = "Cập nhật thành công";
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(request.reason))
                        {
                            commonResponse.Message =
                                "Bạn vui lòng nhập lí do để từ chối bài viết này.";
                            commonResponse.Status = 400;
                            return commonResponse;
                        }
                        post.Status = PostStatus.REJECT;
                        Notification notification = new Notification
                        {
                            Name =
                                "Thông báo quan trọng về việc một bài viết của bạn đã không được sự chấp nhận trên nền tảng.",
                            Content =
                                "Rất tiếc bài post của bạn đã không được chấp thuận vì: "
                                + request.reason,
                            CreatedDate = SettedUpDateTime.GetCurrentVietNamTime(),
                            ReceiverId = post.CreaterId.ToString(),
                            Status = NotificationStatus.NEW,
                            Type = NotificationType.NOTIFYING,
                            Image = notificationImage,
                            DataType = DataNotificationType.POST,
                            DataId = post.Id
                        };
                        await _notificationRepository.CreateNotificationAsync(notification);
                        await _hubContext.Clients.All.SendAsync(
                            post.CreaterId.ToString(),
                            notification
                        );
                        commonResponse.Status = 200;
                        commonResponse.Message = "Cập nhật thành công";
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy bài post.";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreatePost);
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

        public async Task<CommonResponse> UpdatePost(
            Guid postId,
            PostCreatingRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                Post? post = await _postRepository.GetPostById(postId);

                if (post != null && post.Status == PostStatus.REJECT)
                {
                    if (post.CreaterId != userId)
                    {
                        commonResponse.Status = 403;
                        commonResponse.Message = "Bạn không có quyền thực hiện hành động này.";
                        return commonResponse;
                    }

                    post.Content = request.Content;

                    post.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                    List<string> imageUrls = new List<string>();
                    foreach (var a in request.Images)
                    {
                        using (var stream = a.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString() + Path.GetExtension(a.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            imageUrls.Add(imageUrl);
                        }
                    }
                    post.Images = post.Images = string.Join(",", imageUrls);
                    post.Status = PostStatus.UNVERIFIED;
                    await _postRepository.UpdatePostAsync(post);

                    commonResponse.Status = 200;
                    commonResponse.Message = "Cập nhật thành công";
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy bài post.";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreatePost);
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
