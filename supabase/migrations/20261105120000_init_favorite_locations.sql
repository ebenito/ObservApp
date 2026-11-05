-- 1. Crear tabla favorite_locations
CREATE TABLE IF NOT EXISTS public.favorite_locations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  altitude_m DOUBLE PRECISION NOT NULL DEFAULT 0,
  is_favorite BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 2. Índices
CREATE INDEX IF NOT EXISTS idx_favorite_locations_user_id ON public.favorite_locations(user_id);
CREATE INDEX IF NOT EXISTS idx_favorite_locations_name ON public.favorite_locations(name);

-- 3. Row Level Security
ALTER TABLE public.favorite_locations ENABLE ROW LEVEL SECURITY;

-- Eliminar políticas previas si existen antes de crearlas
DROP POLICY IF EXISTS "Users see own favorite locations" ON public.favorite_locations;
DROP POLICY IF EXISTS "Users can insert own favorite locations" ON public.favorite_locations;
DROP POLICY IF EXISTS "Users can update own favorite locations" ON public.favorite_locations;
DROP POLICY IF EXISTS "Users can delete own favorite locations" ON public.favorite_locations;

-- Crear políticas
CREATE POLICY "Users see own favorite locations"
  ON public.favorite_locations FOR SELECT
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own favorite locations"
  ON public.favorite_locations FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own favorite locations"
  ON public.favorite_locations FOR UPDATE
  USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own favorite locations"
  ON public.favorite_locations FOR DELETE
  USING (auth.uid() = user_id);
