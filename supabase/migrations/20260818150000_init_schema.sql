-- 1. Crear tabla observation_sessions
CREATE TABLE IF NOT EXISTS public.observation_sessions (
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
  targets JSONB DEFAULT '[]'::jsonb,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. Índices
CREATE INDEX IF NOT EXISTS idx_observation_sessions_user_id ON public.observation_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_observation_sessions_date ON public.observation_sessions(date DESC);

-- 3. Row Level Security
ALTER TABLE public.observation_sessions ENABLE ROW LEVEL SECURITY;

-- Eliminar políticas previas si existen antes de crearlas
DROP POLICY IF EXISTS "Users see own sessions" ON public.observation_sessions;
DROP POLICY IF EXISTS "Users can insert own sessions" ON public.observation_sessions;
DROP POLICY IF EXISTS "Users can update own sessions" ON public.observation_sessions;
DROP POLICY IF EXISTS "Users can delete own sessions" ON public.observation_sessions;

-- Crear políticas
CREATE POLICY "Users see own sessions" 
  ON public.observation_sessions FOR SELECT 
  USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own sessions" 
  ON public.observation_sessions FOR INSERT 
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own sessions" 
  ON public.observation_sessions FOR UPDATE 
  USING (auth.uid() = user_id) 
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own sessions" 
  ON public.observation_sessions FOR DELETE 
  USING (auth.uid() = user_id);
