using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess.DbContextData
{
    public class FoodDonationDeliveryDbContext : DbContext
    {
        public DbSet<AcceptableAidRequest> AcceptableAidRequests { get; set; }
        public DbSet<AcceptableDonatedRequest> AcceptableDonatedRequests { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityBranch> ActivityBranches { get; set; }
        public DbSet<ActivityFeedback> ActivityFeedbacks { get; set; }
        public DbSet<ActivityMember> ActivityMembers { get; set; }
        public DbSet<ActivityRole> ActivityRoles { get; set; }
        public DbSet<ActivityTask> ActivityTasks { get; set; }
        public DbSet<ActivityType> ActivityTypes { get; set; }
        public DbSet<ActivityTypeComponent> ActivityTypeComponents { get; set; }
        public DbSet<ItemTemplateAttribute> ItemTemplateAttributes { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Charity> Charities { get; set; }
        public DbSet<CharityUnit> CharityUnits { get; set; }
        public DbSet<CollaboratorApplication> CollaboratorApplications { get; set; }
        public DbSet<DeliveryItem> DeliveryItems { get; set; }
        public DbSet<DeliveryRequest> DeliveryRequests { get; set; }
        public DbSet<AidItem> AidItems { get; set; }
        public DbSet<AidRequest> AidRequests { get; set; }
        public DbSet<DonatedItem> DonatedItems { get; set; }
        public DbSet<DonatedRequest> DonatedRequests { get; set; }
        public DbSet<ItemTemplate> ItemTemplates { get; set; }
        public DbSet<ItemCategory> ItemCategories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemAttributeValue> ItemAttributeValues { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Phase> Phases { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleMember> RoleMembers { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RoleTask> RoleTasks { get; set; }
        public DbSet<ScheduledRoute> ScheduledRoutes { get; set; }
        public DbSet<ScheduledRouteDeliveryRequest> ScheduledRouteDeliveryRequests { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockUpdatedHistory> StockUpdatedHistories { get; set; }
        public DbSet<StockUpdatedHistoryDetail> StockUpdatedHistoryDetails { get; set; }
        public DbSet<TargetProcess> TargetProcesses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }

        public DbSet<ItemUnit> ItemUnits { get; set; }

        private readonly IConfiguration? _config = null;

        public FoodDonationDeliveryDbContext() { }

        public FoodDonationDeliveryDbContext(
            DbContextOptions<FoodDonationDeliveryDbContext> options,
            IConfiguration? config = null
        )
            : base(options)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (_config != null)
                    optionsBuilder.UseSqlServer(
                        _config["ConnectionStrings:FoodDonationDeliveryDB"]
                    );
                else
                {
                    try
                    {
                        string projectBPath = Path.GetFullPath(
                            Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "..",
                                "FoodDonationDeliveryManagementAPI"
                            )
                        );
                        IConfiguration config = new ConfigurationBuilder()
                            .SetBasePath(projectBPath)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .Build();
                        optionsBuilder.UseSqlServer(
                            config["ConnectionStrings:FoodDonationDeliveryDB"]
                        );
                    }
                    catch { }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*Map enum fields to string in db*/
            ConfigureEnumAsString<AcceptableAidRequestStatus>(modelBuilder);

            ConfigureEnumAsString<AcceptableDonatedRequestStatus>(modelBuilder);

            ConfigureEnumAsString<ActivityFeedbackStatus>(modelBuilder);

            ConfigureEnumAsString<ActivityMemberStatus>(modelBuilder);

            ConfigureEnumAsString<ActivityRoleStatus>(modelBuilder);

            ConfigureEnumAsString<ActivityScope>(modelBuilder);

            ConfigureEnumAsString<ActivityStatus>(modelBuilder);

            ConfigureEnumAsString<ActivityTaskStatus>(modelBuilder);

            ConfigureEnumAsString<BranchStatus>(modelBuilder);

            ConfigureEnumAsString<CharityStatus>(modelBuilder);

            ConfigureEnumAsString<CharityUnitStatus>(modelBuilder);

            ConfigureEnumAsString<CollaboratorStatus>(modelBuilder);

            ConfigureEnumAsString<DataNotificationType>(modelBuilder);

            ConfigureEnumAsString<DeliveryRequestStatus>(modelBuilder);

            ConfigureEnumAsString<AidItemStatus>(modelBuilder);

            ConfigureEnumAsString<AidRequestStatus>(modelBuilder);

            ConfigureEnumAsString<DonatedItemStatus>(modelBuilder);

            ConfigureEnumAsString<DonatedRequestStatus>(modelBuilder);

            ConfigureEnumAsString<Gender>(modelBuilder);

            ConfigureEnumAsString<ItemCategoryType>(modelBuilder);

            ConfigureEnumAsString<ItemTemplateStatus>(modelBuilder);

            ConfigureEnumAsString<ItemStatus>(modelBuilder);

            ConfigureEnumAsString<NotificationStatus>(modelBuilder);

            ConfigureEnumAsString<NotificationType>(modelBuilder);

            ConfigureEnumAsString<PhaseStatus>(modelBuilder);

            ConfigureEnumAsString<PostCommentStatus>(modelBuilder);

            ConfigureEnumAsString<PostStatus>(modelBuilder);

            ConfigureEnumAsString<ReportType>(modelBuilder);

            ConfigureEnumAsString<RolePermissionStatus>(modelBuilder);

            ConfigureEnumAsString<ScheduledRouteDeliveryRequestStatus>(modelBuilder);

            ConfigureEnumAsString<ScheduledRouteStatus>(modelBuilder);

            ConfigureEnumAsString<StockStatus>(modelBuilder);

            ConfigureEnumAsString<StockUpdatedHistoryType>(modelBuilder);

            ConfigureEnumAsString<UserPermissionStatus>(modelBuilder);

            ConfigureEnumAsString<UserStatus>(modelBuilder);

            ConfigureEnumAsString<ItemTemplateAttributeStatus>(modelBuilder);

            /*Map Id for M-M table*/
            modelBuilder
                .Entity<AcceptableAidRequest>()
                .HasKey(
                    acceptableAidRequest =>
                        new { acceptableAidRequest.BranchId, acceptableAidRequest.AidRequestId }
                );

            modelBuilder
                .Entity<AcceptableDonatedRequest>()
                .HasKey(
                    acceptableDonatedRequest =>
                        new
                        {
                            acceptableDonatedRequest.BranchId,
                            acceptableDonatedRequest.DonatedRequestId
                        }
                );

            modelBuilder
                .Entity<ActivityBranch>()
                .HasKey(
                    activityBranch => new { activityBranch.ActivityId, activityBranch.BranchId }
                );

            modelBuilder
                .Entity<ActivityTypeComponent>()
                .HasKey(
                    activityTypeComponent =>
                        new
                        {
                            activityTypeComponent.ActivityId,
                            activityTypeComponent.ActivityTypeId
                        }
                );

            modelBuilder
                .Entity<ItemAttributeValue>()
                .HasKey(
                    itemTemplateAttributeValue =>
                        new
                        {
                            itemTemplateAttributeValue.ItemId,
                            itemTemplateAttributeValue.AttributeValueId
                        }
                );

            modelBuilder
                .Entity<RoleMember>()
                .HasKey(
                    roleMember => new { roleMember.ActivityRoleId, roleMember.ActivityMemberId }
                );

            modelBuilder
                .Entity<RolePermission>()
                .HasKey(
                    rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId }
                );

            modelBuilder
                .Entity<RoleTask>()
                .HasKey(roleTask => new { roleTask.ActivityRoleId, roleTask.ActivityTaskId });

            modelBuilder
                .Entity<TargetProcess>()
                .HasKey(targetProcess => new { targetProcess.ActivityId, targetProcess.ItemId });

            modelBuilder
                .Entity<UserPermission>()
                .HasKey(
                    userPermission => new { userPermission.UserId, userPermission.PermissionId }
                );

            modelBuilder
                .Entity<ScheduledRouteDeliveryRequest>()
                .HasKey(
                    scheduledRouteDeliveryRequest =>
                        new
                        {
                            scheduledRouteDeliveryRequest.ScheduledRouteId,
                            scheduledRouteDeliveryRequest.DeliveryRequestId
                        }
                );

            /*Auto genarate Guid Id for tables*/
            modelBuilder
                .Entity<Activity>()
                .Property(activity => activity.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ActivityFeedback>()
                .Property(activityFeedback => activityFeedback.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ActivityMember>()
                .Property(activityMember => activityMember.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ActivityRole>()
                .Property(activityRole => activityRole.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ActivityTask>()
                .Property(activityTask => activityTask.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ActivityType>()
                .Property(activityType => activityType.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ItemTemplateAttribute>()
                .Property(attribute => attribute.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<AttributeValue>()
                .Property(attributeValue => attributeValue.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Branch>()
                .Property(branch => branch.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Charity>()
                .Property(charity => charity.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<CharityUnit>()
                .Property(charityUnit => charityUnit.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<CollaboratorApplication>()
                .Property(collaborator => collaborator.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<DeliveryItem>()
                .Property(deliveryItem => deliveryItem.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<DeliveryRequest>()
                .Property(deliveryRequest => deliveryRequest.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<AidItem>()
                .Property(aidItem => aidItem.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<AidRequest>()
                .Property(aidRequest => aidRequest.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<DonatedItem>()
                .Property(donatedItem => donatedItem.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<DonatedRequest>()
                .Property(donatedRequest => donatedRequest.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ItemTemplate>()
                .Property(item => item.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ItemCategory>()
                .Property(itemCategory => itemCategory.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Item>()
                .Property(itemTemplate => itemTemplate.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Notification>()
                .Property(notification => notification.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Permission>()
                .Property(permission => permission.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Phase>()
                .Property(phase => phase.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Post>()
                .Property(post => post.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<PostComment>()
                .Property(postComment => postComment.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Report>()
                .Property(report => report.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Role>()
                .Property(role => role.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ScheduledRoute>()
                .Property(scheduledRoute => scheduledRoute.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<Stock>()
                .Property(stock => stock.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<StockUpdatedHistory>()
                .Property(stockUpdatedHistory => stockUpdatedHistory.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<StockUpdatedHistoryDetail>()
                .Property(stockUpdatedHistoryDetail => stockUpdatedHistoryDetail.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<User>()
                .Property(user => user.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            modelBuilder
                .Entity<ItemUnit>()
                .Property(itemUnit => itemUnit.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            /*Restrict on delete*/
            modelBuilder
                .Entity<RoleMember>()
                .HasOne(roleMember => roleMember.ActivityRole)
                .WithMany(activityRole => activityRole.RoleMembers)
                .HasForeignKey(roleMember => roleMember.ActivityRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<RoleTask>()
                .HasOne(roleTask => roleTask.ActivityTask)
                .WithMany(activityTask => activityTask.RoleTasks)
                .HasForeignKey(roleTask => roleTask.ActivityTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<ItemAttributeValue>()
                .HasOne(itemTemplateAttributeValue => itemTemplateAttributeValue.Item)
                .WithMany(itemTemplate => itemTemplate.ItemAttributeValues)
                .HasForeignKey(itemTemplateAttributeValue => itemTemplateAttributeValue.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<StockUpdatedHistoryDetail>()
                .HasOne(stockUpdatedHistoryDetail => stockUpdatedHistoryDetail.StockUpdatedHistory)
                .WithMany(stockUpdatedHistory => stockUpdatedHistory.StockUpdatedHistoryDetails)
                .HasForeignKey(
                    stockUpdatedHistoryDetail => stockUpdatedHistoryDetail.StockUpdatedHistoryId
                )
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<DeliveryItem>()
                .HasOne(deliveryItem => deliveryItem.DonatedItem)
                .WithMany(donatedItem => donatedItem.DeliveryItems)
                .HasForeignKey(deliveryItem => deliveryItem.DonatedItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<Branch>()
                .HasMany(b => b.AcceptableDonatedRequests)
                .WithOne(adr => adr.Branch)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<Branch>()
                .HasMany(b => b.AcceptableAidRequests)
                .WithOne(aar => aar.Branch)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<User>()
                .HasMany(b => b.Posts)
                .WithOne(aar => aar.Creater)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureEnumAsString<TEnum>(ModelBuilder modelBuilder)
            where TEnum : Enum
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(prop => prop.PropertyType == typeof(TEnum));

                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.ClrType)
                        .Property<TEnum>(property.Name)
                        .HasConversion<string>();
                }
            }
        }
    }
}
