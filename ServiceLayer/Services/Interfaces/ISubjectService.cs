using DataAccessLayer.Entities;

namespace ServiceLayer.Services.Interfaces;

public interface ISubjectService
{
    Task<List<DTOs.SubjectDto>> GetAllAsync();
    Task<DTOs.SubjectDto?> GetByIdAsync(string id);
    Task CreateAsync(string code, string name, string description);
    Task UpdateAsync(string id, string code, string name, string description);
    Task DeleteAsync(string id);
    Task EnsureSeedAsync();
}



