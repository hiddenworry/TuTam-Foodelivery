using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.OpenRouteService;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.OpenRouteService.Response;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    public class UtilsController : Controller
    {
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IConfiguration _config;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IOpenRouteService _openRouteService;

        public UtilsController(
            IFirebaseStorageService firebaseStorageService,
            IConfiguration config,
            IFirebaseNotificationService firebaseNotificationService,
            IOpenRouteService openRouteService
        )
        {
            _firebaseStorageService = firebaseStorageService;
            _config = config;
            _firebaseNotificationService = firebaseNotificationService;
            _openRouteService = openRouteService;
        }

        /// <summary>
        /// Use to create an item. Upload and get the image URL.
        /// </summary>
        /// <remarks>
        /// - **image**: Image (Cannot be empty), (File less than 10 MB and only allow jpg, jpeg, png).
        /// </remarks>
        /// <response code="200">
        /// {
        ///   "status": 200,
        ///   "data": "link",
        ///   "pagination": null,
        ///   "message": null
        /// }
        /// </response>
        /// <response code="400">
        /// {
        ///   "status": 400,
        ///   "data": null,
        ///   "pagination": null,
        ///   "message": "Hệ thống chỉ hỗ trợ các tệp như: .jpg, .jpeg, .png"
        /// }
        /// </response>
        /// <response code="500">If there's an internal server error.</response>
        // [Authorize]
        [HttpPost("/image")]
        public async Task<IActionResult> UploadProductImage([FromForm] ImageUploadRequest request)
        {
            try
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:UserMsg:InternalServerErrorMsg"
                ];
                string uploadImageFailedMsg = _config[
                    "ResponseMessages:UserMsg:UploadImageFailedMsg"
                ];
                if (request != null)
                {
                    int MaxFileSizeMegaBytes = _config.GetValue<int>(
                        "FileUpload:MaxFileSizeMegaBytes"
                    );
                    if (request.image.Length > MaxFileSizeMegaBytes * 1024 * 1024)
                    {
                        return BadRequest(
                            new CommonResponse
                            {
                                Status = 400,
                                Message = $"Dung lượng tối đa phải <= {MaxFileSizeMegaBytes} MB"
                            }
                        );
                    }

                    // Kiểm tra định dạng tệp hình ảnh
                    string[] allowedImageExtensions = _config
                        .GetSection("FileUpload:AllowedImageExtensions")
                        .Get<string[]>();
                    string fileExtension = Path.GetExtension(request.image.FileName).ToLower();
                    if (!allowedImageExtensions.Contains(fileExtension))
                    {
                        string errorMessage =
                            $"Hệ thống chỉ hỗ trợ các tệp như: {string.Join(", ", allowedImageExtensions)}";
                        return BadRequest(
                            new CommonResponse { Status = 400, Message = errorMessage }
                        );
                    }

                    // Tiến hành tải lên và xử lý hình ảnh
                    using (var stream = request.image.OpenReadStream())
                    {
                        string imageName =
                            Guid.NewGuid().ToString() + Path.GetExtension(request.image.FileName);
                        string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                            stream,
                            imageName
                        );

                        if (imageUrl != null)
                        {
                            return Ok(
                                new CommonResponse
                                {
                                    Status = 200,
                                    Message = uploadImageFailedMsg,
                                    Data = imageUrl
                                }
                            );
                        }
                    }
                }
                throw new Exception();
            }
            catch
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                ];
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Use to create an item. Remove the image to upload a different one.
        /// </summary>
        /// <remarks>
        /// - **imageUrl**: Image URL (Cannot be empty).
        /// </remarks>
        /// <response code="200">If successful.</response>
        /// <response code="400">If there's a validation error.</response>
        /// <response code="500">If there's an internal server error.</response>
        [Authorize]
        [HttpDelete("/image")]
        public async Task<IActionResult> DeleteProductImage(string imageUrl)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            string deleteImageSucessfullMsg = _config[
                "ResponseMessages:UserMsg:DeleteImageSucessfullMsg"
            ];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                bool rs = await _firebaseStorageService.DeleteImageAsync(imageUrl);
                if (rs)
                {
                    commonResponse.Status = 200;
                    commonResponse.Message = deleteImageSucessfullMsg;
                    return Ok(commonResponse);
                }
                else
                    throw new Exception();
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        [HttpPost("push-notification")]
        public async Task<IActionResult> PushNotification(
            [FromBody] PushNotificationRequest request
        )
        {
            try
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:UserMsg:InternalServerErrorMsg"
                ];

                bool isSuccess = await _firebaseNotificationService.PushNotification(request);
                if (isSuccess)
                {
                    return Ok(new CommonResponse { Status = 200, Message = "Push Successful" });
                }

                return BadRequest(new CommonResponse { Status = 400, Message = "Push failed" });
            }
            catch
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                ];
                return Ok(new CommonResponse { Status = 500, Message = internalServerErrorMsg });
            }
        }

        [HttpGet("nearby-branch-by-location")]
        public async Task<IActionResult> CheckNearbyBranchByLocation(
            double latitude,
            double longitude
        )
        {
            try
            {
                DeliverableBranches deliverableBranches =
                    await _openRouteService.GetDeliverableBranchesByUserLocation(
                        $"{latitude},{longitude}",
                        null,
                        null
                    );

                return Ok(
                    new CommonResponse
                    {
                        Status = 200,
                        Data = deliverableBranches.NearestBranch != null,
                        Message = _config["ResponseMessages:CommonMsg:CheckLocationSuccessMsg"]
                    }
                );
            }
            catch
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:CommonMsg:InternalServerErrorMsg"
                ];
                return Ok(new CommonResponse { Status = 500, Message = internalServerErrorMsg });
            }
        }
    }
}
