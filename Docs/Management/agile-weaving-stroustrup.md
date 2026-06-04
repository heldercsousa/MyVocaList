# YouTube API Research: Key Acquisition, Quota & UX Strategy — MyVocaList

> **Purpose:** Evaluate the YouTube Data API v3 constraints and their impact on MyVocaList's
> song-search feature across two phases: KJ-only search (Phase 1) and singer self-service search (Phase 2).

---

## 1. API Key Acquisition — What It Takes

Getting a YouTube Data API v3 key is a **purely developer-facing process**:

1. Sign in to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new Cloud project
3. Navigate to API Library → enable "YouTube Data API v3"
4. Go to Credentials → Create credentials → API key
5. (Recommended) Restrict the key to the YouTube Data API only

**Total time:** ~10–15 minutes. Requires a Google account and literacy with developer consoles.

**Verdict for end-user distribution:** Not acceptable UX. Singers and most KJs are not developers.
The risk is real: requiring this step as onboarding will kill app adoption.

---

## 2. Quota — The Hard Numbers

| Metric | Value |
|--------|-------|
| Default daily quota | **10,000 units/day** per Google Cloud project |
| `search.list` cost | **100 units per call** |
| Effective daily searches | **100 searches/day** with the default allocation |
| Quota reset | Midnight Pacific Time (no rollover) |
| Quota scope | Per project — all keys in one project share the pool |

### Quota pressure by phase

| Phase | Who searches | Searches per event (estimate) | Daily budget consumed |
|-------|-------------|------------------------------|----------------------|
| Phase 1 — KJ only | 1 operator | 20–40 (finding tracks, re-searching) | 20–40% |
| Phase 2 — Singers too | KJ + N singers | 3–5 per singer × 10–20 singers = 50–100+ | **100–200% — quota exhausted** |

Phase 2 makes a single 10,000-unit project unsustainable even for one event, let alone a fleet of KJs.

---

## 3. Terms of Service — What Is and Isn't Allowed

- **Asking each KJ to get their own key** → ToS-compliant if each is an independent deployment, but breaks adoption (see § 1).
- **Distributing keys to users to bypass quota** → explicit ToS violation.
- **Multiple Cloud projects to multiply quota** → ToS violation if done systematically.
- **One developer-held key proxied server-side** → fully ToS-compliant; the standard commercial app model.

---

## 4. Architectural Options by Phase

### Phase 1 — KJ-only search (MVP)

**Option A1 — KJ configures their own key once in Settings**
- KJ follows a one-time setup guide (~15 min); key stored in `SecureStorage`
- Each KJ deployment has its own 100 searches/day
- Zero server infrastructure
- **Pro:** ToS-clean, no running costs for Helder, sufficient quota for KJ-only search
- **Con:** Onboarding friction; KJs who aren't tech-comfortable may drop the app

**Option A2 — Helder's key embedded server-side (backend proxy)**
- All installations call Helder's server, which proxies YouTube search
- Zero config for KJs — works out of the box
- **Pro:** Zero friction, cleanest UX
- **Con:** 100 searches/day shared across ALL active KJs worldwide → not viable at any scale without a quota increase or monetization to fund it

**Viable for Phase 1:** Either A1 or A2. A1 is practical if the KJ persona is tech-capable; A2 is better UX but requires infrastructure.

---

### Phase 2 — Singer self-service search

Once singers search, the 100-searches/day model collapses entirely. The only sustainable paths are:

**Option B1 — Backend proxy + monetization (subscription/paid app)**
- Helder runs a server with a pool of YouTube API projects (rotating keys or requesting quota increases)
- Subscription revenue funds API quota + server costs
- **This is the standard model for any SaaS that embeds YouTube search**
- **Pro:** Zero config for everyone, scalable, Helder controls the experience
- **Con:** Helder must build and operate a backend; pricing must cover costs

**Option B2 — Avoid YouTube Data API entirely; use in-app WebView search**
- Open a YouTube search WebView inside the app — no API key, no quota
- KJ or singer taps a result; app captures the video ID from the URL
- **Pro:** Zero API dependency, zero quota, zero config
- **Con:** UX is less polished (native app embedding a web page); ToS grey area; dependent on YouTube's web UI not changing

**Option B3 — Third-party YouTube search proxy (e.g. Supadata, RapidAPI wrappers)**
- Pay a third-party service that wraps YouTube API for simpler access
- Monthly cost replaces quota management
- **Pro:** Simpler than managing Google Cloud projects at scale
- **Con:** Adds a vendor dependency; pricing may be unpredictable as scale grows

---

## 5. Strategic Recommendation

### Phase 1 (now, KJ-only):
**Use Option A1 — one-time API key setup per KJ, configured in Settings.**

Rationale: KJs who install and configure a MAUI app on Android are already technically capable enough for a 15-minute one-time setup. A clear, illustrated in-app setup guide removes the friction. The app should show a friendly "Set up YouTube search in Settings" prompt rather than an error when the key is absent — this frames it as a feature to unlock, not a broken state.

### Phase 2 (singer search):
**The only viable path is Option B1 — backend proxy funded by app monetization.**

This is unavoidable. Singer-driven search means 50–150+ searches per event, per KJ. That quota cannot be covered by free individual API keys without violating ToS. A subscription model (e.g. per-month or per-event fee) naturally funds the API costs and backend infrastructure. This is also what makes zero-config possible for everyone — users pay, Helder provides the key transparently.

### The zero-config / free / no-backend triangle

You can only pick two:

| | Zero-config | Free to user | No backend |
|---|:-----------:|:------------:|:----------:|
| A1 (KJ key in Settings) | ✗ | ✓ | ✓ |
| A2/B1 (Helder's backend) | ✓ | depends on pricing | ✗ |
| B2 (WebView) | ✓ | ✓ | ✓ (but ToS risk) |

---

## 6. BACKLOG Implication

This research suggests the song-search feature needs a **two-stage design**:

- `[Phase 1]` Song search: KJ-only, user-configured API key, graceful degradation when absent
- `[Phase 2]` Song search: singer self-service, backend proxy, requires monetization decision first

Phase 2 is blocked on the **monetization / distribution model decision**, which is an architectural decision Helder must make before any singer-search spec can be written.

---

## Sources

- [YouTube Data API — Getting Started](https://developers.google.com/youtube/v3/getting-started)
- [YouTube API Services Terms of Service](https://developers.google.com/youtube/terms/api-services-terms-of-service)
- [Quota and Compliance Audits](https://developers.google.com/youtube/v3/guides/quota_and_compliance_audits)
- [search.list reference + quota cost](https://developers.google.com/youtube/v3/docs/search/list)
- [Quota Calculator](https://developers.google.com/youtube/v3/determine_quota_cost)
- [YouTube API Limits 2026 — getphyllo.com](https://www.getphyllo.com/post/youtube-api-limits-how-to-calculate-api-usage-cost-and-fix-exceeded-api-quota)
- [Complete Guide to YouTube Data API v3 Quotas — elfsight.com](https://elfsight.com/blog/youtube-data-api-v3-limits-operations-resources-methods-etc/)
- [Unverified Apps — Google API Console Help](https://support.google.com/googleapi/answer/7454865?hl=en)
- [Free YouTube API 2026 — supadata.ai](https://supadata.ai/youtube-api)
