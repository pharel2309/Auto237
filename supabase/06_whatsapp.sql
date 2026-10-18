-- Auto237 — Migration 6 : numéros WhatsApp (optionnels)
-- Exécuter dans Supabase Dashboard > SQL Editor (projet qnsgfykjtddsexvlhlxw)

do $$
begin
  alter table public.profils add column if not exists telephone_whatsapp text;
exception when others then null; end $$;

do $$
begin
  alter table public.vehicules add column if not exists tel_whatsapp text;
exception when others then null; end $$;

comment on column public.profils.telephone_whatsapp is 'Numéro WhatsApp optionnel. NULL = pas de WhatsApp.';
comment on column public.vehicules.tel_whatsapp is 'Numéro WhatsApp figé au moment de la publication. NULL = bouton caché.';