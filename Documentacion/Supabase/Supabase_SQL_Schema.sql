-- Script SQL para crear tabla observation_sessions con Row Level Security (RLS)
-- Ejecutar en Supabase Dashboard > SQL Editor

-- Crear tabla observation_sessions
CREATE TABLE IF NOT EXISTS observation_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  date TIMESTAMPTZ NOT NULL,
  title TEXT NOT NULL,
  notes TEXT,
  latitude DOUBLE PRECISION,
  longitude DOUBLE PRECISION,
  location_name TEXT,
  seeing INTEGER CHECK (seeing >= 1 AND seeing <= 5),
  transparency INTEGER CHECK (transparency >= 1 AND transparency <= 5),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Crear índice para consultas por user_id
CREATE INDEX IF NOT EXISTS idx_observation_sessions_user_id 
  ON observation_sessions(user_id);

-- Crear índice para ordenar por fecha
CREATE INDEX IF NOT EXISTS idx_observation_sessions_date 
  ON observation_sessions(date DESC);

-- Habilitar Row Level Security (RLS)
ALTER TABLE observation_sessions ENABLE ROW LEVEL SECURITY;

-- Eliminar políticas existentes (si las hay)
DROP POLICY IF EXISTS "Users see own sessions" ON observation_sessions;
DROP POLICY IF EXISTS "Users can insert own sessions" ON observation_sessions;
DROP POLICY IF EXISTS "Users can update own sessions" ON observation_sessions;
DROP POLICY IF EXISTS "Users can delete own sessions" ON observation_sessions;

-- Política: Lectura (SELECT)
-- Los usuarios solo pueden ver sus propias sesiones
CREATE POLICY "Users see own sessions"
  ON observation_sessions FOR SELECT
  USING (auth.uid() = user_id);

-- Política: Creación (INSERT)
-- Los usuarios solo pueden crear sesiones para sí mismos
CREATE POLICY "Users can insert own sessions"
  ON observation_sessions FOR INSERT
  WITH CHECK (auth.uid() = user_id);

-- Política: Actualización (UPDATE)
-- Los usuarios solo pueden actualizar sus propias sesiones
CREATE POLICY "Users can update own sessions"
  ON observation_sessions FOR UPDATE
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

-- Política: Eliminación (DELETE)
-- Los usuarios solo pueden eliminar sus propias sesiones
CREATE POLICY "Users can delete own sessions"
  ON observation_sessions FOR DELETE
  USING (auth.uid() = user_id);

-- Trigger para actualizar updated_at automáticamente
CREATE OR REPLACE FUNCTION update_observation_sessions_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS update_observation_sessions_updated_at_trigger 
  ON observation_sessions;

CREATE TRIGGER update_observation_sessions_updated_at_trigger
BEFORE UPDATE ON observation_sessions
FOR EACH ROW
EXECUTE FUNCTION update_observation_sessions_updated_at();


--AÑADO CAMBIOS:

-- Eliminar la columna anterior si era de tipo TEXT
ALTER TABLE observation_sessions DROP COLUMN IF EXISTS targets;

-- Crear la columna como JSONB (con un valor por defecto de lista vacía)
ALTER TABLE observation_sessions ADD COLUMN targets JSONB DEFAULT '[]'::jsonb;






-- =========================
-- Tabla favorite_locations
-- =========================
CREATE TABLE IF NOT EXISTS favorite_locations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  altitude_m DOUBLE PRECISION NOT NULL DEFAULT 0,
  is_favorite BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_favorite_locations_user_id
  ON favorite_locations(user_id);

CREATE INDEX IF NOT EXISTS idx_favorite_locations_name
  ON favorite_locations(name);

ALTER TABLE favorite_locations ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users see own favorite locations" ON favorite_locations;
DROP POLICY IF EXISTS "Users can insert own favorite locations" ON favorite_locations;
DROP POLICY IF EXISTS "Users can update own favorite locations" ON favorite_locations;
DROP POLICY IF EXISTS "Users can delete own favorite locations" ON favorite_locations;

CREATE POLICY "Users see own favorite locations"
  ON favorite_locations FOR SELECT
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own favorite locations"
  ON favorite_locations FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own favorite locations"
  ON favorite_locations FOR UPDATE
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own favorite locations"
  ON favorite_locations FOR DELETE
  USING (auth.uid() = user_id);


-- Doy los permisos necesarios a los usuarios autenticados para acceder a la tabla favorite_locations
GRANT SELECT ON public.favorite_locations TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.favorite_locations TO authenticated;



-- Recargar la caché del esquema de PostgREST
NOTIFY pgrst, 'reload schema';
