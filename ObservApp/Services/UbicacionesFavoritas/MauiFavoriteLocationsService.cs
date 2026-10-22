using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using ObservApp.Shared.Models;
using ObservApp.Shared.Services;
using Supabase;

namespace ObservApp.Services
{
    public class MauiFavoriteLocationsService : IFavoriteLocationsService
    {
        private readonly Client _supabase;
        private readonly SemaphoreSlim _initializeLock = new(1, 1);
        private bool _initialized;
        private const string PrefKey = "favorite_locations";

        public MauiFavoriteLocationsService(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<FavoriteLocation>> GetFavoriteLocationsAsync()
        {
            var local = GetLocalFavoriteLocations();

            try
            {
                await EnsureInitializedAsync();
                var userId = _supabase.Auth.CurrentUser?.Id;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return local;
                }

                var result = await _supabase
                    .From<FavoriteLocation>()
                    .Where(l => l.UserId == userId)
                    .Get();

                var remote = result?.Models ?? new List<FavoriteLocation>();
                SaveLocalFavoriteLocations(remote);
                return remote;
            }
            catch
            {
                return local;
            }
        }

        public async Task SaveFavoriteLocationAsync(FavoriteLocation location)
        {
            var local = GetLocalFavoriteLocations();
            UpsertLocal(local, location);
            SaveLocalFavoriteLocations(local);

            try
            {
                await EnsureInitializedAsync();
                var userId = _supabase.Auth.CurrentUser?.Id;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return;
                }

                location.UserId = userId;
                location.IsFavorite = true;
                if (location.Id == Guid.Empty)
                {
                    location.Id = Guid.NewGuid();
                }

                var existing = await _supabase
                    .From<FavoriteLocation>()
                    .Where(l => l.Id == location.Id && l.UserId == userId)
                    .Single();

                if (existing != null)
                {
                    await _supabase
                        .From<FavoriteLocation>()
                        .Where(l => l.Id == location.Id && l.UserId == userId)
                        .Update(location);
                }
                else
                {
                    await _supabase
                        .From<FavoriteLocation>()
                        .Insert(location);
                }
            }
            catch
            {
                // fallback local ya persistido
            }
        }

        public async Task DeleteFavoriteLocationAsync(Guid id)
        {
            var local = GetLocalFavoriteLocations();
            var removed = local.RemoveAll(l => l.Id == id);
            if (removed > 0)
            {
                SaveLocalFavoriteLocations(local);
            }

            try
            {
                await EnsureInitializedAsync();
                var userId = _supabase.Auth.CurrentUser?.Id;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return;
                }

                await _supabase
                    .From<FavoriteLocation>()
                    .Where(l => l.Id == id && l.UserId == userId)
                    .Delete();
            }
            catch
            {
                // fallback local ya aplicado
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializeLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await _supabase.InitializeAsync();
                _initialized = true;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        private static void UpsertLocal(List<FavoriteLocation> list, FavoriteLocation location)
        {
            var existingIndex = list.FindIndex(l => l.Id == location.Id);
            if (existingIndex >= 0)
            {
                list[existingIndex] = location;
            }
            else
            {
                list.Add(location);
            }
        }

        private static List<FavoriteLocation> GetLocalFavoriteLocations()
        {
            try
            {
                var json = Preferences.Default.Get<string?>(PrefKey, null);
                if (string.IsNullOrEmpty(json))
                {
                    return new List<FavoriteLocation>();
                }

                var dtos = JsonSerializer.Deserialize<List<FavoriteLocationDto>>(json);
                if (dtos == null)
                {
                    return new List<FavoriteLocation>();
                }

                return dtos.Select(FavoriteLocation.FromDto).ToList();
            }
            catch
            {
                return new List<FavoriteLocation>();
            }
        }

        private static void SaveLocalFavoriteLocations(List<FavoriteLocation> list)
        {
            var dtos = list.Select(l => l.ToDto()).ToList();
            var json = JsonSerializer.Serialize(dtos);
            Preferences.Default.Set(PrefKey, json);
        }
    }
}
