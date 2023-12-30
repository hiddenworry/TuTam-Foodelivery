using DataAccess.Entities;
using DataAccess.Models.Requests.OpenRouteService.Request;
using DataAccess.Models.Requests.OpenRouteService.Response;

namespace BusinessLogic.Utils.OpenRouteService
{
    public interface IOpenRouteService
    {
        //Task<List<Branch>> FindNearbyActiveBranchesByCharityUnitLocation(
        //    string charityUnitLocation
        //);
        //Task<Branch?> FindNearestActiveBranchByCharityUnitLocationAsync(string charityUnitLocation);
        //Task<List<Branch>> FindNearbyActiveBranchesByUserLocation(string userLocation);
        //Task<Branch?> FindNearestActiveBranchByUserLocationAsync(string userLocation);
        //Task<Branch?> FindNearestActiveBranchByUserLocationAndAListOfBranchesAsync(
        //    string userLocation,
        //    List<Branch> branches,
        //    double? inputDistance
        //);
        //Task<List<Branch>> FindNearbyActiveBranchesByBranchLocation(string location, Guid id);
        //Task<Branch?> FindNearestActiveBranchByBranchLocationAsync(string location, Guid id);

        Task<DeliverableBranches> GetDeliverableBranchesByUserLocation(
            string userLocation,
            List<Branch>? listedBranches,
            double? maxDistance
        );
        Task<DeliverableBranches> GetDeliverableBranchesByCharityUnitLocation(
            string charityUnitLocation
        );
        Task<DeliverableBranches> GetDeliverableBranchesByBranchLocation(
            string branchLocation,
            Guid branchId
        );

        List<double>? GetCoordinatesByLocation(string location);
        Task<OptimizeResponse?> GetOptimizeResponseAsync(OptimizeRequest optimizeRequest);
        List<double> SwapCoordinates(List<double> coordinates);
    }
}
