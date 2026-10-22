using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
namespace ObservApp.Web.Services
{
    public class SsrFavoriteLocationsService : IFavoriteLocationsService
    {
        public Task<List<FavoriteLocation>> GetFavoriteLocationsAsync()
        {
            return Task.FromResult(new List<FavoriteLocation>());
        }
        public Task SaveFavoriteLocationAsync(FavoriteLocation location)
        {
            return Task.CompletedTask;
        }
        public Task DeleteFavoriteLocationAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }
}
