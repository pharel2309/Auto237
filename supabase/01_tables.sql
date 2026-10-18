-- Auto237 — Schéma Supabase partie 1 : tables
-- Exécuter dans Supabase Dashboard > SQL Editor

do $$ begin
  create type role_utilisateur as enum ('client', 'agence');
exception when duplicate_object then null; end $$;

do $$ begin
  create type boite_vitesse as enum ('manuelle', 'auto');
exception when duplicate_object then null; end $$;

do $$ begin
  create type type_offre as enum ('vente', 'location');
exception when duplicate_object then null; end $$;

do $$ begin
  create type statut_annonce as enum ('publie', 'vendu', 'loue', 'suspendu');
exception when duplicate_object then null; end $$;

create table if not exists public.profils (
  id uuid primary key references auth.users(id) on delete cascade,
  nom text not null,
  telephone text unique,
  role role_utilisateur not null default 'client',
  photo_url text,
  created_at timestamptz not null default now()
);

create table if not exists public.vehicules (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references public.profils(id) on delete cascade,
  titre text not null check (char_length(titre) between 3 and 120),
  marque text not null,
  modele text,
  annee int not null check (annee between 1980 and 2030),
  ville text not null,
  boite boite_vitesse not null default 'manuelle',
  type_offre type_offre not null default 'vente',
  prix_vente bigint check (prix_vente is null or prix_vente > 0),
  prix_jour bigint check (prix_jour is null or prix_jour > 0),
  description text,
  tel_contact text not null,
  statut statut_annonce not null default 'publie',
  vues int not null default 0,
  created_at timestamptz not null default now(),
  check (
    (type_offre = 'vente' and prix_vente is not null)
    or (type_offre = 'location' and prix_jour is not null)
  )
);

create index if not exists idx_vehicules_ville on public.vehicules (ville);
create index if not exists idx_vehicules_boite on public.vehicules (boite);
create index if not exists idx_vehicules_type on public.vehicules (type_offre);
create index if not exists idx_vehicules_created on public.vehicules (created_at desc);

create table if not exists public.photos (
  id uuid primary key default gen_random_uuid(),
  vehicule_id uuid not null references public.vehicules(id) on delete cascade,
  url text not null,
  ordre int not null default 0,
  created_at timestamptz not null default now()
);
create index if not exists idx_photos_vehicule on public.photos (vehicule_id, ordre);

create table if not exists public.favoris (
  user_id uuid not null references public.profils(id) on delete cascade,
  vehicule_id uuid not null references public.vehicules(id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (user_id, vehicule_id)
);
