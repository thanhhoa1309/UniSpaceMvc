using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniSpace.Bo.DTOs.CampusDTOs;
using UniSpace.Model.Entities;
using UniSpace.Model.Interfaces;
using UniSpace.Service.Interfaces;
using UniSpace.Service.Utils;


namespace UniSpace.Service.Services
{
    public class CampusService : ICampusService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<CampusService> _logger;

        public CampusService(
            IUnitOfWork unitOfWork,
            IClaimsService claimsService,
            ICurrentTime currentTime,
            ILogger<CampusService> logger)
        {
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
            _currentTime = currentTime;
            _logger = logger;
        }

        #region Create

        public async Task<CampusDto?> CreateCampusAsync(CreateCampusDto createDto)
        {
            try
            {
                _logger.LogInformation($"Creating new campus: {createDto.Name}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    throw ErrorHelper.BadRequest("Campus name is required");
                }

                // Check if campus name already exists
                if (await CampusNameExistsAsync(createDto.Name))
                {
                    _logger.LogWarning($"Campus name already exists: {createDto.Name}");
                    throw ErrorHelper.Conflict($"Campus with name '{createDto.Name}' already exists");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                var campus = new Campus
                {
                    Id = Guid.NewGuid(),
                    Name = createDto.Name.Trim(),
                    Address = createDto.Address.Trim(),
                    IsDeleted = false,
                    CreatedAt = currentTime,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.Campus.AddAsync(campus);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Campus created successfully: {campus.Id}");

                return MapToDto(campus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating campus: {createDto.Name}");
                throw;
            }
        }

        #endregion

        #region Read

        public async Task<Pagination<CampusDto>> GetCampusesAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving campuses - Page {pageNumber}, Size {pageSize}, Search: '{searchTerm}'");

                // Start with base query
                IQueryable<Campus> query = _unitOfWork.Campus
                    .GetQueryable()
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Rooms);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(c =>
                        c.Name.Contains(searchTerm) ||
                        c.Address.Contains(searchTerm));
                }

                // Order by name for consistency
                query = query.OrderBy(c => c.Name);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var campuses = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var campusDtos = campuses.Select(MapToDto).ToList();

                _logger.LogInformation($"Retrieved {campusDtos.Count} of {totalCount} campuses");
                return new Pagination<CampusDto>(campusDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated campuses");
                throw;
            }
        }

        public async Task<CampusDto?> GetCampusByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Retrieving campus with ID: {id}");

                var campus = await _unitOfWork.Campus
                    .GetByIdAsync(id, c => c.Rooms);

                if (campus == null || campus.IsDeleted)
                {
                    _logger.LogWarning($"Campus not found: {id}");
                    throw ErrorHelper.NotFound($"Campus with ID '{id}' not found");
                }

                return MapToDto(campus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving campus: {id}");
                throw;
            }
        }

        #endregion

        #region Update

        public async Task<CampusDto?> UpdateCampusAsync(UpdateCampusDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating campus: {updateDto.Id}");

                var campus = await _unitOfWork.Campus.GetByIdAsync(updateDto.Id);

                if (campus == null || campus.IsDeleted)
                {
                    _logger.LogWarning($"Campus not found for update: {updateDto.Id}");
                    throw ErrorHelper.NotFound($"Campus with ID '{updateDto.Id}' not found");
                }

                // Check if new name conflicts with existing campus
                if (await CampusNameExistsAsync(updateDto.Name, updateDto.Id))
                {
                    _logger.LogWarning($"Campus name already exists: {updateDto.Name}");
                    throw ErrorHelper.Conflict($"Campus with name '{updateDto.Name}' already exists");
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                campus.Name = updateDto.Name.Trim();
                campus.Address = updateDto.Address.Trim();
                campus.UpdatedAt = currentTime;
                campus.UpdatedBy = currentUserId;

                await _unitOfWork.Campus.Update(campus);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Campus updated successfully: {campus.Id}");

                return MapToDto(campus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating campus: {updateDto.Id}");
                throw;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> SoftDeleteCampusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Soft deleting campus: {id}");

                var campus = await _unitOfWork.Campus.GetByIdAsync(id);

                if (campus == null)
                {
                    _logger.LogWarning($"Campus not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Campus with ID '{id}' not found");
                }

                if (campus.IsDeleted)
                {
                    _logger.LogWarning($"Campus already deleted: {id}");
                    return false;
                }

                var currentUserId = _claimsService.GetCurrentUserId;
                var currentTime = _currentTime.GetCurrentTime();

                await _unitOfWork.Campus.SoftRemove(campus);
                campus.DeletedAt = currentTime;
                campus.DeletedBy = currentUserId;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Campus soft deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting campus: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteCampusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Hard deleting campus: {id}");

                var campus = await _unitOfWork.Campus.GetByIdAsync(id, c => c.Rooms);

                if (campus == null)
                {
                    _logger.LogWarning($"Campus not found for deletion: {id}");
                    throw ErrorHelper.NotFound($"Campus with ID '{id}' not found");
                }

                // Check if campus has rooms
                if (campus.Rooms != null && campus.Rooms.Any())
                {
                    _logger.LogWarning($"Cannot delete campus with existing rooms: {id}");
                    throw ErrorHelper.BadRequest("Cannot delete campus that has rooms. Delete rooms first.");
                }

                await _unitOfWork.Campus.HardRemove(c => c.Id == id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Campus hard deleted successfully: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting campus: {id}");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> CampusExistsAsync(Guid id)
        {
            try
            {
                var campus = await _unitOfWork.Campus.GetByIdAsync(id);
                return campus != null && !campus.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking campus existence: {id}");
                return false;
            }
        }

        public async Task<bool> CampusNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                var campuses = await _unitOfWork.Campus
                    .GetAllAsync(c => !c.IsDeleted && c.Name.ToLower() == name.ToLower().Trim());

                if (excludeId.HasValue)
                {
                    campuses = campuses.Where(c => c.Id != excludeId.Value).ToList();
                }

                return campuses.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking campus name existence: {name}");
                return false;
            }
        }

        private CampusDto MapToDto(Campus campus)
        {
            return new CampusDto
            {
                Id = campus.Id,
                Name = campus.Name,
                Address = campus.Address,
                TotalRooms = campus.Rooms?.Count(r => !r.IsDeleted) ?? 0,
                CreatedAt = campus.CreatedAt
            };
        }

        #endregion
    }
}
