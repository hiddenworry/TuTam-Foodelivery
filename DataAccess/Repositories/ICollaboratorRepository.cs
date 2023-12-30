using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface ICollaboratorRepository
    {
        Task<int>? CreateCollaboratorAsync(CollaboratorApplication collaborator);
        Task<int> DeleteCollaboratorAsync(CollaboratorApplication collaborator);
        Task<List<CollaboratorApplication>?> FindCollaboratorActiveAsync();
        Task<CollaboratorApplication?> FindCollaboratorByIdAsync(Guid collaboratorId);
        Task<CollaboratorApplication?> FindCollaboratorByUserIdAsync(Guid userId);
        Task<List<CollaboratorApplication>?> GetCollaboratorByStatusAsync(
            CollaboratorStatus? status
        );
        Task<int> UpdateCollaboratorAsync(CollaboratorApplication collaborator);
    }
}
