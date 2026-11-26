# Tasks — YouTube without key + MusicBrainz + Wikidata artist images

Plan: `plan.md`. DRY Onion, all serial (shared files — see plan § File ownership chain): T1 → T2a → T2b → T3 → T4 → T5 → T6 → T7.

- [x] T1 MusicBrainzProvider + rate limiter + `ProviderNames` (REQ-MSRC-02, -10)
- [x] T2a Services/Domain: drop Deezer branches, MusicBrainz save path (REQ-MSRC-01, -02, -03, -10) — depends T1
- [~] T2b DI + UI: delete DeezerProvider, MB badge, User-Agent contact from config (REQ-MSRC-01) — depends T2a
- [ ] T3 Migration: clear Deezer provenance in place — depends T2b
- [ ] T4 Remove YouTube Data API, key settings, search page; search-out button (REQ-MSRC-04, -05) — depends T2b
- [ ] T5 YouTube link rules + Android share target (REQ-MSRC-06) — depends T4
- [ ] T6 IFrame player page (REQ-MSRC-07) — depends T5; halt for the on-device check
- [ ] T7 Artist profile: Wikidata photo + credit, description, discography (REQ-MSRC-08, -09) — depends T2b, T6 (shared DI file)
