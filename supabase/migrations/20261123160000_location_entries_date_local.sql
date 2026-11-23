-- Fecha legible y consultable para location_entries.
-- date_ticks (bigint) sigue siendo la fuente de verdad: guarda DateTime.Ticks de
-- TiempoUbicacionApp sin pérdida y la app filtra y ordena con él en memoria.
-- date_local es una columna GENERADA: Postgres la calcula sola en cada INSERT/UPDATE,
-- la app no la escribe y nunca puede quedar desincronizada. Sirve para ver la fecha
-- en el panel de Supabase y para consultar por fecha con SQL, p. ej.:
--   WHERE date_local >= '2025-01-01' AND date_local < '2026-01-01'   -- registros de 2025
--   WHERE extract(month FROM date_local) = 8                          -- agosto de cualquier año
--   WHERE date_local::date = '2025-08-04'                             -- un día concreto
--
-- Conversión: 1 tick = 100 ns desde 0001-01-01; 864000000000 ticks = 1 día. Se separan
-- días y microsegundos del día porque interval * número opera en double precision: con
-- todo en segundos o microsegundos (~6e17 µs) se perderían decenas de microsegundos.
-- Hora local del dispositivo, sin zona horaria (igual que en TiempoUbicacionApp).

ALTER TABLE public.location_entries
  ADD COLUMN IF NOT EXISTS date_local TIMESTAMP
  GENERATED ALWAYS AS (
    CASE WHEN date_ticks > 0 THEN
      TIMESTAMP '0001-01-01 00:00:00'
        + (date_ticks / 864000000000) * INTERVAL '1 day'
        + ((date_ticks % 864000000000) / 10) * INTERVAL '1 microsecond'
    END
  ) STORED;

CREATE INDEX IF NOT EXISTS idx_location_entries_user_date_local
  ON public.location_entries(user_id, date_local DESC);

-- Recargar la caché del esquema de PostgREST
NOTIFY pgrst, 'reload schema';
