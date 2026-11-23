-- Favoritas y ubicaciones del planificador en location_entries.
--
-- is_favorite  true si la ubicación está marcada como favorita (en Gestión de ubicaciones).
-- source       origen de la fila: 'import' (TiempoUbicacionApp), 'favorite' (creada al guardar
--              una favorita) o 'planner' (creada desde el planificador). El mapa la usa para el
--              color de la chincheta. Las filas existentes son importaciones.
-- favorite_id  id de la favorita (favorite_locations.id) enlazada a esta fila; permite
--              actualizarla o desmarcarla cuando se edita o borra la favorita. Sin FK: la
--              favorita puede existir solo en el almacenamiento local del dispositivo.
-- altitude_m   altitud en metros cuando se conoce (favoritas y planificador; TiempoUbicacionApp
--              no la guarda).
--
-- Las columnas tienen valor por defecto, así que las versiones anteriores de la app siguen
-- pudiendo importar.

ALTER TABLE public.location_entries
  ADD COLUMN IF NOT EXISTS is_favorite BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS source TEXT NOT NULL DEFAULT 'import',
  ADD COLUMN IF NOT EXISTS favorite_id UUID,
  ADD COLUMN IF NOT EXISTS altitude_m DOUBLE PRECISION;

ALTER TABLE public.location_entries
  DROP CONSTRAINT IF EXISTS location_entries_source_check;

ALTER TABLE public.location_entries
  ADD CONSTRAINT location_entries_source_check
  CHECK (source IN ('import', 'favorite', 'planner'));

CREATE INDEX IF NOT EXISTS idx_location_entries_user_favorite
  ON public.location_entries(user_id, favorite_id)
  WHERE favorite_id IS NOT NULL;

-- Recargar la caché del esquema de PostgREST
NOTIFY pgrst, 'reload schema';
