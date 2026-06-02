using System;
using System.Collections.Generic;
using System.Text;

namespace ObservApp.Shared.Models
{
    public class FavoriteLocation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AltitudeMeters { get; set; }
    }
}
