# Findings: T1 on-device lock-mechanism spike (B, then C)

> Owned by the orchestrator on `develop` (plan § 0.3 item 3). The T1a implementor returns its facts in
> its handback; the orchestrator records them in § Environment. Helder fills the Result and Evidence
> cells of the run sheet at T1b. The verdict is recorded at T1c.

## Environment

*(filled at T1c from the T1a handback: spike branch + commit SHA, `RequestDelay` type, Context7 results
per member, `devexpress-maui` MCP result, build and emulator smoke results)*

## How to deploy and record (T1b)

**Deploy:** from `.worktrees/acf-t1-spike`, with the device connected over USB:
`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run` (or open that worktree's
`MyVocaList.sln` in VS). Open the flyout and tap **ACF Spike**. For B5, open **Songs → an existing song (edit)**.

**Recording setup (once):**
1. Developer options → **Show taps** ON (taps are then visible in every frame).
2. Record at 60 fps. Preferred: `scrcpy --record=<name>.mp4 --max-fps=60 --no-audio`. Fallback: `adb shell screenrecord --bit-rate 20000000 /sdcard/<name>.mp4`, then `adb pull` (3-minute limit per clip). Record one clip per criterion group, named `t1-<criterion>-<attempt>.mp4`. Keep the clips **outside the repo** (e.g. `C:\Users\helde\Videos\acf-t1\`); they are too large to commit.
3. Frame check: step through frames in the player (VLC: `E` = next frame), or extract them with `ffmpeg -i t1-b2-1.mp4 -vf fps=60 frames/%05d.png`. Confirm the fps with `ffprobe` (`r_frame_rate`). If a clip is below 60 fps, write down the real rate next to the result.
4. Screenshots (B3, R1, S1, S2): device screenshot, same zoom, light theme (and dark theme if time allows).

## T1 run sheet

The criteria are verbatim from `tasks.md` T1 (via `plan.md` § T1b). Helder fills Result and Evidence.

| # | Setup | Action | Pass iff | Result | Evidence (clip/frames/log) |
|---|-------|--------|----------|--------|----------------------------|
| B1 | Field B, "Lock B programmatically" | Tap the text, 5× | No keyboard and no cursor in any frame | | |
| B2 | Field B: type "jo", pick "Johnny Cash", **keyboard still up** | 10 keystrokes + 1 long-press paste | Text byte-equal "Johnny Cash"; 0 frames show a typed/pasted char | | |
| B3 | Field B locked; Reference `TextEdit` beside it | Screenshot both; then unlock Field B, type "jo" and screenshot its built-in clear icon | End icon in `OnSurfaceVariant`, not dimmed. End-icon X (locked) vs built-in clear icon (unlocked): same glyph, size, position, colour? Record every difference | | |
| B4 | Field B locked | Tap X, 5× (re-lock between taps) | Log shows `EndIconClicked` 5/5 | | |
| B5a | Field B | Programmatic lock, 5× | No menu opens (log: no `IsDropDownOpen=true`), 5/5 | | |
| B5b | Spike SongFormPage, edit a saved song with an artist | Open edit, 5× | No menu opens on load, 5/5 | | |
| B5c | Field B and spike SongFormPage | Pick an option, 5× each | No menu reopens after the pick, 5/5 | | |
| B6 | Field B locked | Tap X | Keyboard opens; typing "jo" logs `ItemsRequested` with a search and shows options | | |
| R1 | Field R | Screenshot | Text in the normal (not disabled) colour; no X in any frame | | |
| R2 | Field R | Tap, long-press, 10 keystrokes | Text byte-equal "Don't Cry"; no keyboard, no cursor in any frame | | |
| R3 | Field R | Page load + tap, 5× | No menu opens, 5/5 | | |
| S1 | "Supporting text" ON | Screenshot Field B unlocked, Field B locked, Field R | "Online search unavailable" under each field | | |
| S2 | "Supporting text" ON, then "HasError (component rule)" ON | Screenshot | Only "Title is required" shows, on one line, with no layout jump beyond DX's own bottom line | | |
| S2b | "Supporting text" ON, then "HasError, keep HelpText" ON | Screenshot | Informational: does DX hide `HelpText` by itself while `HasError`? Record yes/no and what renders | | |
| M | Any field | Look | No magnifier icon (`SubmitIconVisibility=Never`) | | |
| H | Field B and Field C | Pick, then reopen the menu by unlocking + typing | Does any row stay highlighted? (per mechanism; informational) | | |
| C1 | *Only if B failed.* Field C locked | 10 keystrokes | Text byte-equal; 0 frames show a typed char | | |
| C2 | *Only if B failed.* Field C locked | Long-press paste; long-press "select all" + keystroke; Gboard swipe one word; accented char via long-press | Each leaves the text byte-equal; 0 frames show a change | | |
| C3 | *Only if B failed.* Field C locked | Type, paste | 0 `ItemsRequested` searches in the log; 0 frames with the menu | | |
| C4 | *Only if B failed.* Field C locked | Tap X, 5× | Clears 5/5; never swallowed by the `Unfocus` | | |
| C5 | *Only if B failed.* Field C locked | Tap the text, 5× | Frames with the keyboard visible (measure; target 0; **Helder sets the threshold**) | | |

**Verdict rule (`design.md` § 5 decision table):** all of B1–B6 pass → **B**. Otherwise, if C1–C5 pass → **escalate Q5**, and C is built only if Helder accepts it after seeing the numbers. Otherwise → **stop and escalate** with options (i)–(iii). If R1–R3 fail, tell Helder regardless of the verdict: the song-form item's read-only Title depends on them.

## Verdict

*(recorded at T1c)*
