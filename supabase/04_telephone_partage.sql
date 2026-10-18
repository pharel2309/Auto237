-- Auto237 — Migration 4 : téléphone partageable (plus d'unicité)
-- Exécuter dans Supabase Dashboard > SQL Editor (projet qnsgfykjtddsexvlhlxw)

-- Supprimer la contrainte UNIQUE sur profils.telephone
do $$
begin
  alter table public.profils drop constraint if exists profils_telephone_key;
exception when others then null; end $$;

-- Index simple (non unique) pour les recherches par téléphone
create index if not exists idx_profils_telephone on public.profils (telephone);

-- Commentaire
comment on column public.profils.telephone is 'Téléphone partagé autorisé entre plusieurs comptes.';