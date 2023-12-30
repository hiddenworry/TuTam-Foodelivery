using BusinessLogic.Services;
using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.EmailService;
using BusinessLogic.Utils.ExcelService;
using BusinessLogic.Utils.ExcelService.Implements;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.FirebaseService.Implements;
using BusinessLogic.Utils.HangfireService;
using BusinessLogic.Utils.HangfireService.Implements;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.OpenRouteService;
using BusinessLogic.Utils.OpenRouteService.Implements;
using BusinessLogic.Utils.SecurityServices;
using BusinessLogic.Utils.SecurityServices.Implements;
using BusinessLogic.Utils.SmsService;
using BusinessLogic.Utils.SmsService.Implements;
using DataAccess.DbContextData;
using DataAccess.Models.Requests.Validators;
using DataAccess.Repositories;
using DataAccess.Repositories.Implements;
using FluentValidation;
using FluentValidation.AspNetCore;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using FoodDonationDeliveryManagementAPI.Security.MiddleWare;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = builder.Configuration.GetValue<string>("LogsFileNamePrefix");
    options.FileSizeLimit = builder.Configuration.GetValue<int>("LogsFileSizeLimit");
    options.RetainedFileCountLimit = builder.Configuration.GetValue<int>(
        "RetainedLogsFileCountLimit"
    );
});
builder.Logging.AddFilter("Microsoft", LogLevel.Warning); // Filter out logs from Microsoft namespaces
builder.Logging.AddFilter("System", LogLevel.Warning); // Filter out logs from System namespaces

// Add services to the container.
//builder.Services
//    .AddControllers()
//    .AddFluentValidation(
//        c => c.RegisterValidatorsFromAssemblyContaining<ActivityCreatingRequestValidator>()
//    );

// Add services to the container.
builder.Services.AddControllers(); // Register MVC Controllers

// FluentValidation configuration
builder.Services
    .AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<ActivityCreatingRequestValidator>();

builder.Services.AddHangfire(
    config =>
        config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                builder.Configuration.GetValue<string>("ConnectionStrings:FoodDonationDeliveryDB")
            )
);
builder.Services.AddHangfireServer();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<FoodDonationDeliveryDbContext>();

//builder.Services.AddDbContext<FoodDonationDeliveryDbContext>(options =>
//{
//    options.UseSqlServer(builder.Configuration.GetValue<string>("ConnectionStrings:FoodDonationDeliveryDB"));

//});

builder.Services.AddScoped<IAcceptableAidRequestRepository, AcceptableAidRequestRepository>();
builder.Services.AddScoped<
    IAcceptableDonatedRequestRepository,
    AcceptableDonatedRequestRepository
>();

builder.Services.AddScoped<IActivityBranchRepository, ActivityBranchRepository>();
builder.Services.AddScoped<IActivityFeedbackRepository, ActivityFeedbackRepository>();
builder.Services.AddScoped<IActivityMemberRepository, ActivityMemberRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityRoleRepository, ActivityRoleRepository>();
builder.Services.AddScoped<IActivityTaskRepository, ActivityTaskRepository>();
builder.Services.AddScoped<IActivityTypeComponentRepository, ActivityTypeComponentRepository>();
builder.Services.AddScoped<IActivityTypeRepository, ActivityTypeRepository>();
builder.Services.AddScoped<IItemTemplateAttributeRepository, ItemTemplateAttributeRepository>();
builder.Services.AddScoped<IAttributeValueRepository, AttributeValueRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<ICharityRepository, CharityRepository>();
builder.Services.AddScoped<ICharityUnitRepository, CharityUnitRepository>();
builder.Services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
builder.Services.AddScoped<IDeliveryItemRepository, DeliveryItemRepository>();
builder.Services.AddScoped<IDeliveryRequestRepository, DeliveryRequestRepository>();
builder.Services.AddScoped<IAidItemRepository, AidItemRepository>();
builder.Services.AddScoped<IAidRequestRepository, AidRequestRepository>();
builder.Services.AddScoped<IDonatedItemRepository, DonatedItemRepository>();
builder.Services.AddScoped<IDonatedRequestRepository, DonatedRequestRepository>();
builder.Services.AddScoped<IItemCategoryRepository, ItemCategoryRepository>();
builder.Services.AddScoped<IItemTemplateRepository, ItemTemplateRepository>();
builder.Services.AddScoped<IItemAttributeValueRepository, ItemAttributeValueRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IPhaseRepository, PhaseRepository>();
builder.Services.AddScoped<IPostCommentRepository, PostCommentRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IRoleMemberRepository, RoleMemberRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleTaskRepository, RoleTaskRepository>();
builder.Services.AddScoped<
    IScheduledRouteDeliveryRequestRepository,
    ScheduledRouteDeliveryRequestRepository
>();
builder.Services.AddScoped<IScheduledRouteRepository, ScheduledRouteRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<
    IStockUpdatedHistoryDetailRepository,
    StockUpdatedHistoryDetailRepository
>();
builder.Services.AddScoped<IStockUpdatedHistoryRepository, StockUpdatedHistoryRepository>();
builder.Services.AddScoped<ITargetProcessRepository, TargetProcessRepository>();
builder.Services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IItemUnitRepostitory, ItemUnitRepository>();

builder.Services.AddScoped<IAcceptableAidRequestService, AcceptableAidRequestService>();
builder.Services.AddScoped<IAcceptableDonatedRequestService, AcceptableDonatedRequestService>();
builder.Services.AddScoped<IActivityBranchService, ActivityBranchService>();
builder.Services.AddScoped<IActivityFeedbackService, ActivityFeedbackService>();
builder.Services.AddScoped<IActivityMemberService, ActivityMemberService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IActivityRoleService, ActivityRoleService>();
builder.Services.AddScoped<IActivityTaskService, ActivityTaskService>();
builder.Services.AddScoped<IActivityTypeComponentService, ActivityTypeComponentService>();
builder.Services.AddScoped<IActivityTypeService, ActivityTypeService>();
builder.Services.AddScoped<IItemTemplateAttributeService, ItemTemplateAttributeService>();
builder.Services.AddScoped<IAttributeValueService, AttributeValueService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ICharityService, CharityService>();
builder.Services.AddScoped<ICharityUnitService, CharityUnitService>();
builder.Services.AddScoped<ICollaboratorService, CollaboratorService>();
builder.Services.AddScoped<IDeliveryItemService, DeliveryItemService>();
builder.Services.AddScoped<IDeliveryRequestService, DeliveryRequestService>();
builder.Services.AddScoped<IAidItemService, AidItemService>();
builder.Services.AddScoped<IAidRequestService, AidRequestService>();
builder.Services.AddScoped<IDonatedItemService, DonatedItemService>();
builder.Services.AddScoped<IDonatedRequestService, DonatedRequestService>();
builder.Services.AddScoped<IItemCategoryService, ItemCategoryService>();
builder.Services.AddScoped<IItemTemplateService, ItemTemplateService>();
builder.Services.AddScoped<IItemAttributeValueService, ItemAttributeValueService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPhaseService, PhaseService>();
builder.Services.AddScoped<IPostCommentService, PostCommentService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRoleMemberService, RoleMemberService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoleTaskService, RoleTaskService>();
builder.Services.AddScoped<IScheduledRouteService, ScheduledRouteService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockUpdatedHistoryDetailService, StockUpdatedHistoryDetailService>();
builder.Services.AddScoped<IStockUpdatedHistoryService, StockUpdatedHistoryService>();
builder.Services.AddScoped<ITargetProcessService, TargetProcessService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IItemUnitService, ItemUnitService>();

builder.Services.AddScoped<IOpenRouteService, OpenRouteService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISMSService, SMSService>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
builder.Services.AddSingleton<IHangfireService, HangfireService>();
builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

// Configure for security
string issuer = builder.Configuration.GetValue<string>("JwtConfig:Issuer");
string signingKey = builder.Configuration.GetValue<string>("JwtConfig:SecretKey");
TimeSpan tokenLifetime = TimeSpan.FromMinutes(
    builder.Configuration.GetValue<int>("JwtConfig:ExpiredTimeMinutes")
);
byte[] signingKeyBytes = System.Text.Encoding.UTF8.GetBytes(signingKey);

builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        //options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = issuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = tokenLifetime,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
        };
    });

//AuthorizationConfig.LoadPermissionsFromDatabase(builder.Services);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Configuration.AddJsonFile($"msgconfig.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddMemoryCache();
builder.Services.AddTransient<ITokenBlacklistService, TokenBlackListService>();
builder.Services.AddTransient<TokenValidationMiddleware>();
builder.Services.AddTransient<PermissionMiddleware>();
builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    PermissionAuthorizationPolicyProvider
>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization();
builder.Services.AddCors(o =>
{
    o.AddPolicy(
        "CorsPolicy",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});

// signal R
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo { Title = "FoodDonationDeliveryManagementAPI", Version = "v1" }
    );
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        }
    );
    c.DocumentFilter<BasePathFilter>();
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();
app.UseHttpsRedirection();
app.MapHub<NotificationSignalSender>("/notificationHub");
app.MapHub<ScheduleRouteHub>("/schedule-route-hub");

//Write local logs
var loggerFactory = app.Services.GetService<ILoggerFactory>();
loggerFactory.AddFile(
    builder.Configuration["LogFilePath"].ToString(),
    fileSizeLimitBytes: builder.Configuration.GetValue<int>("LogsFileSizeLimit"),
    retainedFileCountLimit: builder.Configuration.GetValue<int>("RetainedLogsFileCountLimit")
);

app.Map(
    builder.Configuration.GetValue<string>("ApiPrefix"),
    app =>
    {
        app.UseRouting();
        app.UseCors(
            x =>
                x.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true) // allow any origin
                    .AllowCredentials()
        );
        app.UseMiddleware<TokenValidationMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<PermissionMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
);

//app.UseWhen(
//    context => context.Request.Path.StartsWithSegments("/hangfire"),
//    appBuilder =>
//    {
//        appBuilder.UseMiddleware<TokenValidationMiddleware>();
//    }
//);

//Hangfire usage
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions
    {
        Authorization = new[] { new MyAuthorizationFilter() }, // You can define authorization rules
        DashboardTitle = "My Hangfire Dashboard"
    }
);
app.MapHangfireDashboard();

//BackgroundJob.Enqueue(() => app.Services.GetService<IActivityService>()!.TestHangfire());

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
});

app.UseCors(
    x =>
        x.AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true) // allow any origin
            .AllowCredentials()
);

RecurringJob.AddOrUpdate(
    "UpdateAvailableAndLateScheduledRouteBackGround",
    () => app.Services.GetService<IHangfireService>()!.AutoUpdateAvailableAndLateScheduledRoute(),
    "00 17 * * *"
);

RecurringJob.AddOrUpdate(
    "SendNotificationStockOutdate",
    () => app.Services.GetService<IHangfireService>()!.UpdateStockWhenStockOutDate(),
    "00 17 * * *"
);

RecurringJob.AddOrUpdate(
    "UpdateOutDateDonatedRequestsBackGround",
    () => app.Services.GetService<IHangfireService>()!.UpdateOutDateDonatedRequests(),
    "00 17 * * *"
);

RecurringJob.AddOrUpdate(
    "UpdateOutDateAidRequestsBackGround",
    () => app.Services.GetService<IHangfireService>()!.UpdateOutDateAidRequests(),
    "00 17 * * *"
);

//RecurringJob.AddOrUpdate(
//    "CheckLateScheduleRoute",
//    () => app.Services.GetService<IHangfireService>()!.AutoCheckLateScheduleRoute(),
//    Cron.Daily
//);

app.Run();

public class BasePathFilter : IDocumentFilter
{
    private readonly IConfiguration _config;

    public BasePathFilter(IConfiguration config)
    {
        _config = config;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();

        foreach (var entry in swaggerDoc.Paths)
        {
            paths.Add($"{_config["ApiPrefix"]}{entry.Key}", entry.Value);
        }

        swaggerDoc.Paths = paths;
    }
}
