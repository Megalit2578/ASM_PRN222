using ServiceLayer.Dtos;

namespace ServiceLayer.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}


