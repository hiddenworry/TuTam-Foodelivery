using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests.OpenRouteService.Request;
using DataAccess.Models.Requests.OpenRouteService.Response;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace BusinessLogic.Utils.OpenRouteService.Implements
{
    public class OpenRouteService : IOpenRouteService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OpenRouteService> _logger;
        private readonly IBranchRepository _branchRepository;

        public OpenRouteService(
            IConfiguration config,
            IUserRepository userRepository,
            ILogger<OpenRouteService> logger,
            IBranchRepository branchRepository
        )
        {
            _config = config;
            _userRepository = userRepository;
            _logger = logger;
            _branchRepository = branchRepository;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_config["OpenRoute:BasePath"]);
            _httpClient.DefaultRequestHeaders.Add("Authorization", _config["OpenRoute:ApiKey"]);
        }

        //public async Task<List<Branch>> FindNearbyActiveBranchesByCharityUnitLocation(
        //    string charityUnitLocation
        //)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    List<Branch> nearbyBranches = new List<Branch>();
        //    double defaultRoadLengthAsMeters = double.Parse(
        //        _config["OpenRoute:DefaultRoadLengthAsMeters"]
        //    );
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(branch.Location)!,
        //                    GetCoordinatesByLocation(charityUnitLocation)!
        //                )
        //            ).GetValueOrDefault();

        //            if (distance <= defaultRoadLengthAsMeters)
        //            {
        //                nearbyBranches.Add(branch);
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearbyBranches;
        //}

        //public async Task<Branch?> FindNearestActiveBranchByUserLocationAndAListOfBranchesAsync(
        //    string userLocation,
        //    List<Branch> branches,
        //    double? inputDistance
        //)
        //{
        //    Branch? nearestBranch = null;
        //    double? nearstRoadLength = null;
        //    double maxRoadLengthAsMeters = (double)(
        //        inputDistance != null
        //            ? inputDistance
        //            : double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"])
        //    );

        //    branches = branches.Where(b => b.Status == BranchStatus.ACTIVE).ToList();
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(userLocation)!,
        //                    GetCoordinatesByLocation(branch.Location)!
        //                )
        //            ).GetValueOrDefault();

        //            if (
        //                distance <= maxRoadLengthAsMeters
        //                    ? (nearstRoadLength != null ? nearstRoadLength > distance : true)
        //                    : false
        //            )
        //            {
        //                nearstRoadLength = distance;
        //                nearestBranch = branch;
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearestBranch;
        //}

        //public async Task<Branch?> FindNearestActiveBranchByCharityUnitLocationAsync(
        //    string charityUnitLocation
        //)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    Branch? nearestBranch = null;
        //    double? nearstRoadLength = null;
        //    double maxRoadLengthAsMeters = double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]);
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(branch.Location)!,
        //                    GetCoordinatesByLocation(charityUnitLocation)!
        //                )
        //            ).GetValueOrDefault();

        //            if (
        //                distance <= maxRoadLengthAsMeters
        //                    ? (nearstRoadLength != null ? nearstRoadLength > distance : true)
        //                    : false
        //            )
        //            {
        //                nearstRoadLength = distance;
        //                nearestBranch = branch;
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearestBranch;
        //}


        //public async Task<List<Branch>> FindNearbyActiveBranchesByBranchLocation(
        //    string branchLocation,
        //    Guid branchId
        //)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    branches = branches.Where(b => b.Id != branchId).ToList();
        //    List<Branch> nearbyBranches = new List<Branch>();
        //    double defaultRoadLengthAsMeters = double.Parse(
        //        _config["OpenRoute:DefaultRoadLengthAsMeters"]
        //    );
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(branch.Location)!,
        //                    GetCoordinatesByLocation(branchLocation)!
        //                )
        //            ).GetValueOrDefault();

        //            if (distance <= defaultRoadLengthAsMeters)
        //            {
        //                nearbyBranches.Add(branch);
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearbyBranches;
        //}

        //public async Task<Branch?> FindNearestActiveBranchByBranchLocationAsync(
        //    string branchLocation,
        //    Guid branchId
        //)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    branches = branches.Where(b => b.Id != branchId).ToList();
        //    Branch? nearestBranch = null;
        //    double? nearstRoadLength = null;
        //    double maxRoadLengthAsMeters = double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]);
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(branch.Location)!,
        //                    GetCoordinatesByLocation(branchLocation)!
        //                )
        //            ).GetValueOrDefault();

        //            if (
        //                distance <= maxRoadLengthAsMeters
        //                    ? (nearstRoadLength != null ? nearstRoadLength > distance : true)
        //                    : false
        //            )
        //            {
        //                nearstRoadLength = distance;
        //                nearestBranch = branch;
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearestBranch;
        //}

        public List<double>? GetCoordinatesByLocation(string location)
        {
            try
            {
                string coordinatesString = location.Split("-")[0];
                double latitude = double.Parse(coordinatesString.Split(",")[0]);
                double longitude = double.Parse(coordinatesString.Split(",")[1]);
                return new List<double> { latitude, longitude };
            }
            catch
            {
                return null;
            }
        }

        public async Task<double?> GetRoadLengthBetweenTwoCoordinates(
            List<double> fromCoordinates,
            List<double> toCoordinate
        )
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"v2/directions/cycling-electric?start={string.Join(",", SwapCoordinates(fromCoordinates))}&end={string.Join(",", SwapCoordinates(toCoordinate))}"
                );
                string jsonResponseString = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(jsonResponseString);
                double? distance = (double?)
                    jsonResponse["features"]![0]!["properties"]!["summary"]!["distance"];

                if (distance == null)
                    distance = (double)
                        jsonResponse["features"]![0]!["properties"]!["segments"]![0]!["distance"]!;
                return distance;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred in service OpenRouteService, method GetRoadLengthBetweenTwoCoordinates: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }

        public List<double> SwapCoordinates(List<double> coordinates)
        {
            double tmp = coordinates[0];
            coordinates[0] = coordinates[1];
            coordinates[1] = tmp;
            return coordinates;
        }

        //public async Task<List<Branch>> FindNearbyActiveBranchesByUserLocation(string userLocation)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    branches = branches.ToList();

        //    List<Branch> nearbyBranches = new List<Branch>();
        //    double defaultRoadLengthAsMeters = double.Parse(
        //        _config["OpenRoute:DefaultRoadLengthAsMeters"]
        //    );
        //    foreach (Branch branch in branches)
        //    {
        //        try
        //        {
        //            double distance = (
        //                await GetRoadLengthBetweenTwoCoordinates(
        //                    GetCoordinatesByLocation(userLocation)!,
        //                    GetCoordinatesByLocation(branch.Location)!
        //                )
        //            ).GetValueOrDefault();

        //            if (distance <= defaultRoadLengthAsMeters)
        //            {
        //                nearbyBranches.Add(branch);
        //            }
        //        }
        //        catch { }
        //    }
        //    return nearbyBranches;
        //}

        //public async Task<Branch?> FindNearestActiveBranchByUserLocationAsync(string userLocation)
        //{
        //    List<Branch> branches = (
        //        await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null)
        //    ).ToList();
        //    branches = branches.ToList();

        //    return await FindNearestActiveBranchByUserLocationAndAListOfBranchesAsync(
        //        userLocation,
        //        branches,
        //        null
        //    );
        //}

        public async Task<OptimizeResponse?> GetOptimizeResponseAsync(
            OptimizeRequest optimizeRequest
        )
        {
            try
            {
                string jsonRequest = JsonConvert.SerializeObject(
                    optimizeRequest,
                    new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }
                );

                JObject ConvertKeysToCamelCase(JObject jsonObject)
                {
                    var result = new JObject();
                    foreach (var property in jsonObject.Properties())
                    {
                        var camelCaseKey =
                            char.ToLower(property.Name[0]) + property.Name.Substring(1);

                        if (property.Value is JObject nestedObject)
                        {
                            result[camelCaseKey] = ConvertKeysToCamelCase(nestedObject);
                        }
                        else if (property.Value is JArray jsonArray)
                        {
                            var convertedArray = new JArray(
                                jsonArray.Select(item =>
                                {
                                    if (item is JObject nestedArrayObject)
                                    {
                                        return ConvertKeysToCamelCase(nestedArrayObject);
                                    }
                                    return item;
                                })
                            );
                            result[camelCaseKey] = convertedArray;
                        }
                        else
                        {
                            result[camelCaseKey] = property.Value;
                        }
                    }
                    return result;
                }

                JObject parsedJson = JObject.Parse(jsonRequest);
                JObject camelCaseJson = ConvertKeysToCamelCase(parsedJson);

                string camelCaseJsonString = camelCaseJson.ToString(Formatting.None);

                StringContent httpContent = new StringContent(
                    camelCaseJsonString,
                    encoding: Encoding.UTF8,
                    "application/json"
                );
                HttpResponseMessage response = await _httpClient.PostAsync(
                    "optimization",
                    httpContent
                );

                string jsonResponseString = await response.Content.ReadAsStringAsync();
                OptimizeResponse? optimizeResponse =
                    JsonConvert.DeserializeObject<OptimizeResponse>(jsonResponseString);

                if (optimizeResponse != null)
                {
                    if (optimizeResponse.Unassigned == null)
                        optimizeResponse.Unassigned = new List<UnassignedShipment>();
                    if (optimizeResponse.Routes == null)
                        return null;
                    else
                        return optimizeResponse;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<DeliverableBranches> GetDeliverableBranchesByUserLocation(
            string userLocation,
            List<Branch>? listedBranches,
            double? maxDistance
        )
        {
            List<Branch> branches =
                listedBranches != null
                    ? listedBranches
                    : await _branchRepository.GetBranchesAsync(null, BranchStatus.ACTIVE, null);

            List<Branch> nearbyBranches = new List<Branch>();

            double minRoadLengthAsMeters = double.Parse(
                _config["OpenRoute:DefaultRoadLengthAsMeters"]
            );
            double maxRoadLengthAsMeters = (double)(
                maxDistance != null
                    ? maxDistance
                    : double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"])
            );

            Branch? nearestBranch = null;
            double? nearstRoadLength = null;

            foreach (Branch branch in branches)
            {
                try
                {
                    double distance = (
                        await GetRoadLengthBetweenTwoCoordinates(
                            GetCoordinatesByLocation(userLocation)!,
                            GetCoordinatesByLocation(branch.Location)!
                        )
                    ).GetValueOrDefault();

                    if (
                        distance <= maxRoadLengthAsMeters
                            ? nearstRoadLength != null
                                ? nearstRoadLength > distance
                                : true
                            : false
                    )
                    {
                        nearstRoadLength = distance;
                        nearestBranch = branch;
                    }

                    if (distance <= minRoadLengthAsMeters)
                    {
                        nearbyBranches.Add(branch);
                    }
                }
                catch { }
            }

            return new DeliverableBranches
            {
                NearestBranch = nearestBranch,
                NearbyBranches = nearbyBranches
            };
        }

        public async Task<DeliverableBranches> GetDeliverableBranchesByCharityUnitLocation(
            string charityUnitLocation
        )
        {
            List<Branch> branches = await _branchRepository.GetBranchesAsync(
                null,
                BranchStatus.ACTIVE,
                null
            );

            List<Branch> nearbyBranches = new List<Branch>();

            double minRoadLengthAsMeters = double.Parse(
                _config["OpenRoute:DefaultRoadLengthAsMeters"]
            );

            double maxRoadLengthAsMeters = double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]);

            Branch? nearestBranch = null;
            double? nearstRoadLength = null;

            foreach (Branch branch in branches)
            {
                try
                {
                    double distance = (
                        await GetRoadLengthBetweenTwoCoordinates(
                            GetCoordinatesByLocation(branch.Location)!,
                            GetCoordinatesByLocation(charityUnitLocation)!
                        )
                    ).GetValueOrDefault();

                    if (
                        distance <= maxRoadLengthAsMeters
                            ? nearstRoadLength != null
                                ? nearstRoadLength > distance
                                : true
                            : false
                    )
                    {
                        nearstRoadLength = distance;
                        nearestBranch = branch;
                    }

                    if (distance <= minRoadLengthAsMeters)
                    {
                        nearbyBranches.Add(branch);
                    }
                }
                catch { }
            }

            return new DeliverableBranches
            {
                NearestBranch = nearestBranch,
                NearbyBranches = nearbyBranches
            };
        }

        public async Task<DeliverableBranches> GetDeliverableBranchesByBranchLocation(
            string branchLocation,
            Guid branchId
        )
        {
            List<Branch> branches = await _branchRepository.GetBranchesAsync(
                null,
                BranchStatus.ACTIVE,
                null
            );

            branches = branches.Where(b => b.Id != branchId).ToList();

            List<Branch> nearbyBranches = new List<Branch>();

            double minRoadLengthAsMeters = double.Parse(
                _config["OpenRoute:DefaultRoadLengthAsMeters"]
            );

            double maxRoadLengthAsMeters = double.Parse(_config["OpenRoute:MaxRoadLengthAsMeters"]);

            Branch? nearestBranch = null;
            double? nearstRoadLength = null;

            foreach (Branch branch in branches)
            {
                try
                {
                    double distance = (
                        await GetRoadLengthBetweenTwoCoordinates(
                            GetCoordinatesByLocation(branch.Location)!,
                            GetCoordinatesByLocation(branchLocation)!
                        )
                    ).GetValueOrDefault();

                    if (
                        distance <= maxRoadLengthAsMeters
                            ? nearstRoadLength != null
                                ? nearstRoadLength > distance
                                : true
                            : false
                    )
                    {
                        nearstRoadLength = distance;
                        nearestBranch = branch;
                    }

                    if (distance <= minRoadLengthAsMeters)
                    {
                        nearbyBranches.Add(branch);
                    }
                }
                catch { }
            }

            return new DeliverableBranches
            {
                NearestBranch = nearestBranch,
                NearbyBranches = nearbyBranches
            };
        }
    }
}
