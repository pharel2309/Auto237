-- Auto237 — Migration 5 : nom_agence (unique insensible à la casse)
-- Exécuter dans Supabase Dashboard > SQL Editor (projet qnsgfykjtddsexvlhlxw)

-- Ajouter la colonne nom_agence
do $$
begin
  alter table public.profils add column if not exists nom_agence text;
exception when others then null; end $$;

-- Index unique insensible à la casse : "Auto Prestige" = "auto prestige"
create unique index if not exists ux_profils_nom_agence_lower
  on public.profils (lower(nom_agence))
  where nom_agence is not null;

-- Index simple pour recherche
create index if not exists idx_profils_nom_agence on public.profils (nom_agence);

-- Commentaire
comment on column public.profils.nom_agence is 'Nom d\'agence unique, insensible à la casse. NULL pour les clients.';