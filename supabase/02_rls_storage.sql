-- Auto237 partie 2 : RLS tables + Storage
alter table public.profils enable row level security;
alter table public.vehicules enable row level security;
alter table public.photos enable row level security;
alter table public.favoris enable row level security;

drop policy if exists "profils lecture publique" on public.profils;
create policy "profils lecture publique" on public.profils
for select to anon, authenticated using (true);
drop policy if exists "profils insert owner" on public.profils;
create policy "profils insert owner" on public.profils
for insert to authenticated with check (auth.uid() = id);
drop policy if exists "profils update owner" on public.profils;
create policy "profils update owner" on public.profils
for update to authenticated using (auth.uid() = id);

drop policy if exists "vehicules lecture publique" on public.vehicules;
create policy "vehicules lecture publique" on public.vehicules
for select to anon, authenticated using (statut = 'publie' or auth.uid() = user_id);
drop policy if exists "vehicules insert owner" on public.vehicules;
create policy "vehicules insert owner" on public.vehicules
for insert to authenticated with check (auth.uid() = user_id);
drop policy if exists "vehicules update owner" on public.vehicules;
create policy "vehicules update owner" on public.vehicules
for update to authenticated using (auth.uid() = user_id);
drop policy if exists "vehicules delete owner" on public.vehicules;
create policy "vehicules delete owner" on public.vehicules
for delete to authenticated using (auth.uid() = user_id);

drop policy if exists "photos lecture publique" on public.photos;
create policy "photos lecture publique" on public.photos
for select to anon, authenticated using (true);
drop policy if exists "photos insert owner annonce" on public.photos;
create policy "photos insert owner annonce" on public.photos
for insert to authenticated with check (
  exists (select 1 from public.vehicules v where v.id = vehicule_id and v.user_id = auth.uid())
);
drop policy if exists "photos delete owner annonce" on public.photos;
create policy "photos delete owner annonce" on public.photos
for delete to authenticated using (
  exists (select 1 from public.vehicules v where v.id = vehicule_id and v.user_id = auth.uid())
);

drop policy if exists "favoris owner tout" on public.favoris;
create policy "favoris owner tout" on public.favoris
for all to authenticated using (auth.uid() = user_id) with check (auth.uid() = user_id);

-- Storage : bucket public SANS policy SELECT large (corrige alerte listage).
-- Un bucket public est lisible par URL sans SELECT. On garde insert/delete proteges.
drop policy if exists "annonces lecture publique" on storage.objects;
drop policy if exists "annonces upload connecte" on storage.objects;
create policy "annonces upload connecte" on storage.objects
for insert to authenticated with check (bucket_id = 'annonces');
drop policy if exists "annonces delete owner" on storage.objects;
create policy "annonces delete owner" on storage.objects
for delete to authenticated using (bucket_id = 'annonces');
