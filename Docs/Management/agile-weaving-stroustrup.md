# YouTube API Research: Key Acquisition & Quota — MyVocaList Context

> **Purpose:** Evaluate whether requiring end users to obtain their own YouTube Data API v3 key is
> feasible and acceptable UX for MyVocaList. Inform architectural decision for the song-search feature.

---

## 1. How API Key Acquisition Works (2026)

Getting a YouTube Data API v3 key is a **purely developer-facing process**:

1. Sign in to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new Cloud project
3. Navigate to API Library → enable "YouTube Data API v3"
4. Go to Credentials → Create credentials → API key
5. (Recommended) Restrict the key to the YouTube Data API

**Total time:** ~10–15 minutes. Requires a Google account and familiarity with developer consoles.

**Verdict on asking end users to do this:** Unacceptable UX. This is a developer workflow — singers and KJs have no context for Google Cloud projects, API libraries, or credential scopes.

---

## 2. Quota Limits — Critical Numbers

| Metric | Value |
|--------|-------|
| Default daily quota | **10,000 units/day** per Google Cloud project |
| `search.list` cost | **100 units per call** |
| Effective search limit | **100 searches/day** with default quota |
| Quota reset | Midnight Pacific Time (no rollover) |
| Quota scope | Per project (all API keys in the same project share the pool) |

### What this means for a karaoke event

A busy evening with a KJ doing 20–30 song searches (finding tracks for singers, re-searching on bad results) consumes 2,000–3,000 units — 20–30% of the daily quota per event. A single intense session with multiple artists browsing songs could exhaust the 100-search-per-day limit.

### Requesting higher quota

Possible, but requires a compliance audit by Google showing the project adheres to YouTube API Terms of Service. Not guaranteed, and adds operational overhead. Not viable for a lean MVP.

---

## 3. Terms of Service — Per-User Key Distribution

**Distributing individual API keys to end users to bypass quota is a ToS violation.**

> "Splitting work across multiple Google Cloud projects (each with its own quota) to systematically bypass limits violates Google's Terms of Service."

The legitimate model: **one developer-owned project, one key, all users share that quota pool.**

Asking each KJ to get their own key is only ToS-compliant if each KJ is truly running their own independent deployment (not sharing infrastructure). For a distributed app installed per-operator, this is technically fine — but the UX problem remains: KJs are not developers.

---

## 4. OAuth vs API Key — Which Is Needed?

For **song search only** (read-only YouTube search), an **API key suffices** — no OAuth needed.

OAuth is only required for write operations (uploading videos, managing playlists). However, if OAuth were used:
- Unverified apps are **limited to 100 unique users** until Google verifies the app
- Verification requires a formal app review process (weeks)
- Users see a scary "unverified app" warning screen

MyVocaList avoids this entirely by sticking to API key + read-only `search.list`.

---

## 5. Architectural Options for MyVocaList

### Option A — Helder's key embedded / configured once by the operator (KJ)
- KJ sets up their own Google Cloud key once during app installation (Settings page)
- Singers never touch it; it's invisible to them
- Each KJ deployment has its own 100 searches/day
- **Pro:** ToS compliant, no infrastructure cost, simple
- **Con:** KJ must follow a one-time setup guide; ~15 min onboarding tax

### Option B — Backend proxy server (Helder holds the key server-side)
- All MyVocaList instances call Helder's server, which proxies to YouTube
- Users have zero configuration
- **Pro:** Zero UX friction
- **Con:** Requires server infrastructure + hosting costs + quota shared across all users → 100 searches/day total across all KJs is not viable at scale

### Option C — No YouTube API key; use Invidious/Piped proxy
- Invidious/Piped are open-source YouTube frontends with public APIs — no API key required
- **Pro:** Zero key management, no quota
- **Con:** Against YouTube ToS, unreliable (public instances go down), not suitable for a commercial/distributed product

### Option D — YouTube iFrame search (no API key)
- Use YouTube's web search embed or direct URL pattern (`youtube.com/results?search_query=...`) scraped via WebView
- **Pro:** No API key
- **Con:** Fragile (depends on YouTube web UI not changing), violates ToS, poor data quality

### Option E — Defer YouTube integration; use manual URL entry only
- KJ or singers paste a YouTube URL manually; app doesn't search YouTube at all
- **Pro:** No API dependency whatsoever
- **Con:** UX regression — copy-pasting URLs is cumbersome mid-event

---

## 6. Recommendation

**Option A is the right call for MyVocaList's current stage.**

Rationale:
- MyVocaList's primary user persona for song search is the **KJ/operator**, not the singers. Singers don't search — the KJ finds and queues tracks.
- A one-time setup of a YouTube API key by the KJ (who is technically capable enough to install and configure an Android app) is an acceptable onboarding cost.
- 100 searches/day per KJ deployment is sufficient for a typical evening event (KJs rarely exceed 20–30 unique searches).
- This avoids all server infrastructure, ToS risk, and OAuth complexity.
- If quota becomes a real constraint at a specific event, the KJ can request a free quota increase from Google.

**Implementation note:** The API key should be stored via `SecureStorage` on the device and configurable from a Settings page. The song-search feature should gracefully degrade (show a "Configure YouTube API key in Settings" prompt) rather than crashing when no key is set.

---

## 7. Open Questions for Helder

1. Is the target user for song search always the KJ/operator, or will singers also search? (Affects quota pressure significantly.)
2. Is MyVocaList intended to be distributed to multiple independent KJs, or is it Helder's personal tool? (Affects whether Option B becomes viable later.)
3. Is a one-time Settings-page API key setup acceptable, or is zero-config a hard requirement?

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
- [YouTube API Guide 2026 — zernio.com](https://zernio.com/blog/youtube-api)
