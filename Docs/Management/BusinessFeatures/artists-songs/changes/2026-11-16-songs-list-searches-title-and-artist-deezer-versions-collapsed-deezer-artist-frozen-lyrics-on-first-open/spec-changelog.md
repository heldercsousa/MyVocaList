# Spec changelog

| Date | Change | Reason | Requirements |
|------|--------|--------|--------------|
| 2026-11-16 | Section E added: loading placeholder, Save ignored while loading, failed load closes the form | Helder after the first merge: "wrap the load. Also, a loading indicator without freezing UI" | REQ-SLD-32 to 34 |
| 2026-11-17 | A short search text matches inside title or artist name instead of the title prefix | Helder's device check: one or two typed characters showed "No results found" for titles that contain them | REQ-SLD-35 (replaces part of 01, 02) |
| 2026-11-17 | The title of a Deezer song is fixed in edit mode; the Service refuses a change | Helder's device check: he changed the title of a Deezer song while trying to search another song and ended with a title that does not belong to the fixed artist | REQ-SLD-36 to 38, 40 (replace 17) |
| 2026-11-17 | In add mode a changed Title drops the Deezer pick for good | Helder: the known gap C25 must be closed | REQ-SLD-39 (replaces part of 14) |
