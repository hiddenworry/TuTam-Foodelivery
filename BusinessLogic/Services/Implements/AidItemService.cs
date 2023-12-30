using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.Enum;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BusinessLogic.Services.Implements
{
    public class AidItemService : IAidItemService
    {
        private readonly IAidItemRepository _aidItemRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<AidItemService> _logger;
        private readonly IAidRequestRepository _aidRequestRepository;
        private readonly IItemRepository _itemRepository;

        public AidItemService(
            IAidItemRepository aidItemRepository,
            IBranchRepository branchRepository,
            IConfiguration config,
            ILogger<AidItemService> logger,
            IAidRequestRepository aidRequestRepository,
            IItemRepository itemRepository
        )
        {
            _aidItemRepository = aidItemRepository;
            _branchRepository = branchRepository;
            _config = config;
            _logger = logger;
            _aidRequestRepository = aidRequestRepository;
            _itemRepository = itemRepository;
        }

        public async Task<CommonResponse> GetAidItemsForBranchAdminAsync(
            string? fullKeyWord,
            UrgencyLevel? urgencyLevel,
            DateTime? startDate,
            DateTime? endDate,
            Guid? charityUnitId,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            Guid userId
        )
        {
            Branch? branch = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);
            if (branch == null)
            {
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config["ResponseMessages:BranchMsg:BranchNotFoundMsg"]
                };
            }
            else if (branch.Status == BranchStatus.INACTIVE)
            {
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config["ResponseMessages:BranchMsg:InactiveBranchMsg"]
                };
            }

            List<AidRequest> aidRequests =
                await _aidRequestRepository.GetAidRequestsOfCharityUnitWhichHasAppliableItemsToActivityAsync(
                    startDate,
                    endDate,
                    branch.Id,
                    charityUnitId
                );

            List<AidItem> aidItems = new List<AidItem>();
            List<GroupOfAidItemsWithUrgentLevel> groupOfAidItemsWithUrgentLevels =
                new List<GroupOfAidItemsWithUrgentLevel>
                {
                    new GroupOfAidItemsWithUrgentLevel
                    {
                        UrgencyLevel = UrgencyLevel.VERY_URGENT,
                        AidItems = new List<AidItem>()
                    },
                    new GroupOfAidItemsWithUrgentLevel
                    {
                        UrgencyLevel = UrgencyLevel.URGENT,
                        AidItems = new List<AidItem>()
                    },
                    new GroupOfAidItemsWithUrgentLevel
                    {
                        UrgencyLevel = UrgencyLevel.NOT_URGENT,
                        AidItems = new List<AidItem>()
                    }
                };

            aidRequests.ForEach(
                (ar) =>
                {
                    List<AidItem> tmp = ar.AidItems
                        .Where(ai => ai.Status == AidItemStatus.ACCEPTED)
                        .ToList();

                    foreach (AidItem item in tmp)
                    {
                        item.AidRequest = ar;
                    }

                    aidItems.AddRange(tmp);

                    UrgencyLevel? urgencyLevel = GetUrgentLevelOfAidRequest(ar);
                    if (urgencyLevel != null)
                        groupOfAidItemsWithUrgentLevels.ForEach(
                            (g) =>
                            {
                                if (g.UrgencyLevel == urgencyLevel)
                                    g.AidItems.AddRange(ar.AidItems);
                            }
                        );
                }
            );

            foreach (AidItem ai in aidItems)
            {
                ai.Item = (await _itemRepository.FindItemByIdForAidItemAsync(ai.ItemId))!;
            }

            aidItems = aidItems
                .Where(
                    ai =>
                        (
                            urgencyLevel != null
                                ? GetUrgentLevelOfAidItem(ai, groupOfAidItemsWithUrgentLevels)
                                    == urgencyLevel
                                : GetUrgentLevelOfAidItem(ai, groupOfAidItemsWithUrgentLevels)
                                    == UrgencyLevel.VERY_URGENT
                                    || GetUrgentLevelOfAidItem(ai, groupOfAidItemsWithUrgentLevels)
                                        == UrgencyLevel.URGENT
                                    || GetUrgentLevelOfAidItem(ai, groupOfAidItemsWithUrgentLevels)
                                        == UrgencyLevel.NOT_URGENT
                        )
                        && GetAidPeriodOfAidRequest(ai.AidRequest).Count > 0
                )
                .ToList();

            List<Item> results = new List<Item>();
            Dictionary<AidItem, int> Points = new Dictionary<AidItem, int>();
            List<AidItem> tmpAidItems = new List<AidItem>();

            if (fullKeyWord != null)
            {
                List<string> keyWords = fullKeyWord.Split(' ').ToList();
                foreach (string keyWord in keyWords)
                {
                    tmpAidItems = aidItems
                        .Where(
                            i =>
                                i.Item.ItemTemplate.Name.ToLower().Contains(keyWord.ToLower())
                                || i.Item.ItemAttributeValues.Any(
                                    atv =>
                                        atv.AttributeValue.Value
                                            .ToLower()
                                            .Contains(keyWord.ToLower())
                                )
                        )
                        .ToList();

                    foreach (var aidItem in tmpAidItems)
                    {
                        if (Points.ContainsKey(aidItem))
                        {
                            Points[aidItem] += CalculateScore(aidItem, keyWord);
                        }
                        else
                        {
                            Points[aidItem] = CalculateScore(aidItem, keyWord);
                        }
                    }
                }
                aidItems = Points.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
            }

            if (
                orderBy != null
                && sortType != null
                && (sortType == SortType.ASC || sortType == SortType.DES)
            )
            {
                try
                {
                    if (orderBy == "AidPeriod")
                    {
                        if (sortType == SortType.ASC)
                            aidItems = aidItems
                                .OrderBy(ai => GetEndDateOfAidItem(ai, aidRequests))
                                .ToList();
                        else
                            aidItems = aidItems
                                .OrderByDescending(ai => GetEndDateOfAidItem(ai, aidRequests))
                                .ToList();
                    }
                    else
                    {
                        if (sortType == SortType.ASC)
                            aidItems = aidItems
                                .OrderBy(ai => GetPropertyValue(ai, orderBy))
                                .ToList();
                        else
                            aidItems = aidItems
                                .OrderByDescending(ai => GetPropertyValue(ai, orderBy))
                                .ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"An exception occurred in service {nameof(AidItemService)} method {nameof(GetAidItemsForBranchAdminAsync)}."
                    );
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:CommonMsg:SortedFieldNotFoundMsg"]
                    };
                }
            }

            Pagination pagination = new Pagination();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = aidItems.Count;
            aidItems = aidItems
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            return new CommonResponse
            {
                Status = 200,
                Data = aidItems.Select(
                    ai =>
                        new AidItemForActivityResponse
                        {
                            Id = ai.Id,
                            Name = GetFullNameOfItem(ai.Item),
                            Category = ai.Item.ItemTemplate.ItemCategory.Name,
                            Quantity = ai.Quantity,
                            Unit = ai.Item.ItemTemplate.Unit.Name,
                            UrgentLevel = GetUrgentLevelOfAidRequest(ai.AidRequest)!.ToString()!,
                            CharityUnit = ai.AidRequest.CharityUnit!.Name,
                            AidPeriod = GetAidPeriodOfAidRequest(ai.AidRequest)
                        }
                ),
                Pagination = pagination,
                Message = _config["ResponseMessages:AidItemMsg:GetAidItemsSuccessMsg"]
            };
        }

        static object? GetPropertyValue(object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
                return propertyInfo.GetValue(obj);
            else
                throw new Exception(
                    $"Property {propertyName} not found in object type {obj.GetType}."
                );
        }

        private string GetFullNameOfItem(Item item)
        {
            return $"{item.ItemTemplate.Name}"
                + (
                    item.ItemAttributeValues.Count > 0
                        ? $" {string.Join(", ", item.ItemAttributeValues.Select(iav => iav.AttributeValue.Value))}"
                        : ""
                );
        }

        private UrgencyLevel? GetUrgentLevelOfAidItem(
            AidItem aidItem,
            List<GroupOfAidItemsWithUrgentLevel> groupOfAidItemsWithUrgentLevels
        )
        {
            foreach (
                GroupOfAidItemsWithUrgentLevel groupOfAidItemsWithUrgentLevel in groupOfAidItemsWithUrgentLevels
            )
            {
                if (
                    groupOfAidItemsWithUrgentLevel.AidItems.Select(ai => ai.Id).Contains(aidItem.Id)
                )
                    return groupOfAidItemsWithUrgentLevel.UrgencyLevel;
            }
            return null;
        }

        private UrgencyLevel? GetUrgentLevelOfAidRequest(AidRequest aidRequest)
        {
            List<ScheduledTime>? scheduledTimes = JsonConvert.DeserializeObject<
                List<ScheduledTime>
            >(aidRequest.ScheduledTimes);
            if (scheduledTimes == null)
                return null;

            ScheduledTime? lastScheduledTime = GetLastAvailabeScheduledTime(scheduledTimes);
            if (lastScheduledTime == null)
                return null;

            DateTime endDateTime = GetEndDateTimeFromScheduledTime(lastScheduledTime);

            double timeRemainingAsDay = (
                endDateTime - SettedUpDateTime.GetCurrentVietNamTime()
            ).TotalDays;

            if (timeRemainingAsDay > 0)
            {
                if (timeRemainingAsDay <= 3)
                    return UrgencyLevel.VERY_URGENT;
                else if (timeRemainingAsDay <= 7)
                    return UrgencyLevel.URGENT;
                else
                    return UrgencyLevel.NOT_URGENT;
            }
            else
                return null;
        }

        //private string? GetAidPeriodOfAidItem(AidItem aidItem, List<AidRequest> aidRequests)
        //{
        //    return GetAidPeriodOfAidRequest(
        //        aidRequests.FirstOrDefault(
        //            ar => ar.AidItems.Select(ai => ai.Id).Contains(aidItem.Id)
        //        )!
        //    );
        //}

        private DateTime GetEndDateOfAidItem(AidItem aidItem, List<AidRequest> aidRequests)
        {
            return GetEndDateTimeFromScheduledTime(
                GetLastAvailabeScheduledTime(
                    JsonConvert.DeserializeObject<List<ScheduledTime>>(
                        aidRequests
                            .FirstOrDefault(
                                ar => ar.AidItems.Select(ai => ai.Id).Contains(aidItem.Id)
                            )!
                            .ScheduledTimes
                    )!
                )!
            );
        }

        private List<DateTime> GetAidPeriodOfAidRequest(AidRequest aidRequest)
        {
            List<ScheduledTime>? scheduledTimes = JsonConvert.DeserializeObject<
                List<ScheduledTime>
            >(aidRequest.ScheduledTimes);
            if (scheduledTimes == null)
                return new List<DateTime>();

            ScheduledTime? firstScheduledTime = GetFirstAvailabeScheduledTime(scheduledTimes);
            if (firstScheduledTime == null)
                return new List<DateTime>();

            ScheduledTime? lastScheduledTime = GetLastAvailabeScheduledTime(scheduledTimes);
            if (lastScheduledTime == null)
                return new List<DateTime>();

            DateTime startDate = GetStartDateTimeFromScheduledTime(firstScheduledTime);
            DateTime endDate = GetEndDateTimeFromScheduledTime(lastScheduledTime);

            if (startDate == endDate)

                return new List<DateTime> { startDate };
            else
                return new List<DateTime> { startDate, endDate };
            //return $"{startDateTime.ToString("dd-MM-yyyy")}-{endDateTime.ToString("dd-MM-yyyy")}";
        }

        private ScheduledTime? GetFirstAvailabeScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes
                .Where(
                    st =>
                        GetStartDateTimeFromScheduledTime(st)
                        > SettedUpDateTime.GetCurrentVietNamTime()
                )
                .MinBy(st => GetStartDateTimeFromScheduledTime(st));
        }

        private ScheduledTime? GetLastAvailabeScheduledTime(List<ScheduledTime> scheduledTimes)
        {
            return scheduledTimes
                .Where(
                    st =>
                        GetEndDateTimeFromScheduledTime(st)
                        > SettedUpDateTime.GetCurrentVietNamTime()
                )
                .MaxBy(st => GetEndDateTimeFromScheduledTime(st));
        }

        private DateTime GetStartDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            return day.ToDateTime(startTime);
        }

        private DateTime GetEndDateTimeFromScheduledTime(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);
            return day.ToDateTime(endTime);
        }

        private int CalculateScore(AidItem aidItem, string keyWord)
        {
            int score = 0;

            if (aidItem.Item.ItemTemplate.Name.ToLower().Contains(keyWord.ToLower()))
            {
                score += 5;
            }

            if (
                aidItem.Item.ItemAttributeValues.Any(
                    atv => atv.AttributeValue.Value.ToLower().Contains(keyWord.ToLower())
                )
            )
            {
                score += 2;
            }

            return score;
        }
    }

    public class GroupOfAidItemsWithUrgentLevel
    {
        public UrgencyLevel UrgencyLevel { get; set; }

        public string AidPeriod { get; set; }

        public List<AidItem> AidItems { get; set; }
    }
}
