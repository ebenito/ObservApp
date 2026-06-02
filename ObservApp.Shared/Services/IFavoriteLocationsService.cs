using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ObservApp.Shared.Models;
namespace ObservApp.Shared.Services
{
    public interface IFavoriteLocationsService
    {
        Task<List<FavoriteLocation>> GetFavoriteLocationsAsync();
        Task SaveFavoriteLocationAsync(FavoriteLocation location);
        Task DeleteFavoriteLocationAsync(Guid id);
    }
}
