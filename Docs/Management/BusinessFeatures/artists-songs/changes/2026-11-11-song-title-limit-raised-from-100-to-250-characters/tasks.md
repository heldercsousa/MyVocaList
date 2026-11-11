# Tasks — Song title limit raised from 100 to 250 characters

- [x] **1 Spec ritual** — add a pointer note to the SFT requirements lines 215 and 411 ("limit superseded by this change"). Docs on develop.
- [ ] **2 Tests first (Red)** — boundary tests 250 valid / 251 invalid; counter 250/250; long-title search test (AC-STL-01, 02, 04). Level A.
- [ ] **3 Service** — `MaxTitleLength => 250`; update affected message-string tests. [after 2]
- [ ] **4 EF config + migration** — `Song.Title` max length 250; inspect the generated migration for table rebuild (see design). Sequential-only. [after 3]
- [ ] **5 Verification** — full `MyVocaList.Tests`; Helder device check: a 250-char title saves, 251 shows the error.
