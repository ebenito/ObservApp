using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
namespace ObservApp.Services
{
    public class MauiFavoriteLocationsService : IFavoriteLocationsService
    {
        private const string PrefKey = "favorite_locations";
        public Task<List<FavoriteLocation>> GetFavoriteLocationsAsync()
        {
            try
            {
                var json = Preferences.Default.Get<string?>(PrefKey, null);
                if (string.IsNullOrEmpty(json))
                {
                    return Task.FromResult(new List<FavoriteLocation>());
                }
                var list = JsonSerializer.Deserialize<List<FavoriteLocation>>(json);
                return Task.FromResult(list ?? new List<FavoriteLocation>());
            }
            catch
            {
                return Task.FromResult(new List<FavoriteLocation>());
            }
        }
        public async Task SaveFavoriteLocationAsync(FavoriteLocation location)
        {
            var list = await GetFavoriteLocationsAsync();

            var existingIndex = list.FindIndex(l => l.Id == location.Id);
            if (existingIndex >= 0)
            {
                list[existingIndex] = location;
            }
            else
            {
                list.Add(location);
            }
            var json = JsonSerializer.Serialize(list);
            Preferences.Default.Set(PrefKey, json);
        }
        public async Task DeleteFavoriteLocationAsync(Guid id)
        {
            var list = await GetFavoriteLocationsAsync();
            var removed = list.RemoveAll(l => l.Id == id);
            if (removed > 0)
            {
                var json = JsonSerializer.Serialize(list);
                Preferences.Default.Set(PrefKey, json);
            }
        }
    }
}
