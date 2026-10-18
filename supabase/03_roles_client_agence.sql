-- Auto237 — Migration 2 rôles : client + agence (suppression proprietaire)
-- Exécuter dans Supabase Dashboard > SQL Editor (projet qnsgfykjtddsexvlhlxw)

-- 1. Reclasse les anciens propriétaires en clients
update public.profils set role = 'client' where role = 'proprietaire';

-- 2. Change le défaut au cas où
alter table public.profils alter column role set default 'client';

-- 3. Recrée l'enum sans 'proprietaire' (Postgres ne permet pas de retirer une valeur directement)
do $$ begin
  create type role_utilisateur_new as enum ('client', 'agence');
exception when duplicate_object then null; end $$;

alter table public.profils alter column role type role_utilisateur_new
  using (role::text::role_utilisateur_new);

drop type if exists role_utilisateur;
alter type role_utilisateur_new rename to role_utilisateur;
