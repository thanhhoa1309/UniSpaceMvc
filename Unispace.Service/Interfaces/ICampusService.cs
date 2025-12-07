using UniSpace.Bo.DTOs.CampusDTOs;
using UniSpace.Service.Utils;

namespace UniSpace.Service.Interfaces
{
    public interface ICampusService
    {
        // Create
        Task<CampusDto?> CreateCampusAsync(CreateCampusDto createDto);

        // Read - Unified method with filters
        Task<Pagination<CampusDto>> GetCampusesAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null);

        Task<CampusDto?> GetCampusByIdAsync(Guid id);

        // Update
        Task<CampusDto?> UpdateCampusAsync(UpdateCampusDto updateDto);

        // Delete
        Task<bool> DeleteCampusAsync(Guid id);
        Task<bool> SoftDeleteCampusAsync(Guid id);

        // Additional
        Task<bool> CampusExistsAsync(Guid id);
        Task<bool> CampusNameExistsAsync(string name, Guid? excludeId = null);
    }
}
