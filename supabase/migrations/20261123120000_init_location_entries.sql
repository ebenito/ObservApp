-- Registros de ubicación compatibles con TiempoUbicacionApp.
-- Réplica de su tabla SQLite "LocationEntry" (sqlite-net, archivo Ubicaciones.db):
--   Id integer PK autoincrement | Date bigint (DateTime.Ticks, hora local) |
--   fechaActual, horaLocal, horaUtc, desfase, tipoHorario, Location, Latitude, Longitude varchar |
--   Zona integer
-- Los textos se guardan tal cual para que la exportación devuelva el mismo contenido.
-- latitude_deg / longitude_deg son la conversión a grados decimales que usa el mapa
-- (nulas si el texto no se pudo interpretar).

-- 1. Tabla
CREATE TABLE IF NOT EXISTS public.location_entries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  date_ticks BIGINT NOT NULL DEFAULT 0,
  fecha_actual TEXT NOT NULL DEFAULT '',
  hora_local TEXT NOT NULL DEFAULT '',
  hora_utc TEXT NOT NULL DEFAULT '',
  desfase TEXT NOT NULL DEFAULT '',
  tipo_horario TEXT NOT NULL DEFAULT '',
  location TEXT NOT NULL DEFAULT '',
  latitude TEXT NOT NULL DEFAULT '',
  longitude TEXT NOT NULL DEFAULT '',
  zona INTEGER NOT NULL DEFAULT 0,
  latitude_deg DOUBLE PRECISION CHECK (latitude_deg BETWEEN -90 AND 90),
  longitude_deg DOUBLE PRECISION CHECK (longitude_deg BETWEEN -180 AND 180),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Índices
CREATE INDEX IF NOT EXISTS idx_location_entries_user_date
  ON public.location_entries(user_id, date_ticks DESC);

-- 3. Row Level Security
ALTER TABLE public.location_entries ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own location entries" ON public.location_entries;
DROP POLICY IF EXISTS "Users can insert own location entries" ON public.location_entries;
DROP POLICY IF EXISTS "Users can update own location entries" ON public.location_entries;
DROP POLICY IF EXISTS "Users can delete own location entries" ON public.location_entries;

CREATE POLICY "Users see own location entries"
  ON public.location_entries FOR SELECT
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own location entries"
  ON public.location_entries FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own location entries"
  ON public.location_entries FOR UPDATE
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own location entries"
  ON public.location_entries FOR DELETE
  USING (auth.uid() = user_id);

-- 4. Permisos
GRANT SELECT, INSERT, UPDATE, DELETE ON public.location_entries TO authenticated;
