# BUG-043 Phase 0 Spike Findings

**Date:** 2026-07-14  
**Objective:** Disambiguate root cause of zero suggestions on release S23 device  
**Candidates:** H-A (Release trimming) vs H-B (Device IME composition)

---

## 1. Build Configuration Analysis

**File inspected:** `MyVocaList/MyVocaList.csproj`

### Trimming Status

**Release build configuration (lines 69-71):**
```xml
<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net10.0-android|AnyCPU'">
  <AndroidKeyStore>False</AndroidKeyStore>
</PropertyGroup>
```

**Findings:**
- No explicit `TrimMode`, `PublishTrimmed`, `AndroidLinkMode`, `EnableTrimAnalyzer`, or `TrimmerRootAssembly` properties set
- **.NET MAUI 10 enables trimming by default for Release Android builds** — absence of explicit settings means defaults are active
- Trimming is **confirmed active** in Release configuration

**Verdict:** H-A risk is **HIGH**.

---

## 2. Debouncer Threading Analysis

**File inspected:** `MyVocaList/UI/Components/AutocompleteField/AutocompleteDebouncer.cs`

### Threading Flow

**Constructor (line 16-19):**
```csharp
internal AutocompleteDebouncer(Action<Action> dispatcher = null)
{
    _dispatcher = dispatcher ?? (a => MainThread.BeginInvokeOnMainThread(a));
}
```

**Trigger method (line 38-47):**
```csharp
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(delayMs, token);
        if (token.IsCancellationRequested) return;
        _dispatcher(() => onElapsed?.Invoke(text));  // ← Marshaled to UI thread
    }
    catch (OperationCanceledException) { /* ignore */ }
}, token);
```

**Analysis:**
- Delay happens on background thread (`Task.Run`)
- Callback is **correctly marshaled back to UI thread** via `MainThread.BeginInvokeOnMainThread`
- Threading is **sound**

**Verdict:** Threading model is correct. No evidence of race conditions or callback loss.

---

## 3. Component Instantiation Architecture

**Files inspected:**
- `AutocompleteField.xaml.cs` (lines 208-215)
- `AutocompleteMobileField.xaml.cs` (definition)
- `PersonFormPage.xaml` (consumer)

### Key Finding: Dynamic Instantiation + Runtime Binding

**In `AutocompleteField.xaml.cs`, line 208:**
```csharp
var mobileField = new AutocompleteMobileField();
```

**Followed by runtime binding setup (lines 212-215):**
```csharp
mobileField.SetBinding(AutocompleteMobileField.TextProperty,
    new Binding(nameof(Text), BindingMode.TwoWay, source: this));
mobileField.SetBinding(AutocompleteMobileField.SuggestionsProperty,
    new Binding(nameof(Suggestions), source: this));
```

### Static vs Dynamic References

| Component | Reference Type | Trimming Risk |
|-----------|---|---|
| `AutocompleteField` | **Static XAML** in PersonFormPage (line 8, 19) | LOW — XAML compiler preserves it |
| `AutocompleteMobileField` | **Dynamic C# instantiation only** (no XAML reference anywhere) | **HIGH** — linker has no proof it's needed |

### The Trimming Vulnerability

1. **PersonFormPage XAML** statically references `AutocompleteField` → compiler preserves it
2. **AutocompleteField XAML** (`AutocompleteField.xaml`) does **not** reference `AutocompleteMobileField` statically
3. **AutocompleteField.xaml.cs** instantiates `AutocompleteMobileField` dynamically at runtime
4. **Linker analysis:** `AutocompleteMobileField` has no static reference in any XAML → marked as unreachable → **candidate for trimming**
5. **At runtime in Release:** Either the type is trimmed or its property metadata is missing → binding fails → zero suggestions

### Why It Works on Debug

Debug builds disable trimming → all types and properties preserved regardless of references.

---

## 4. Test Matrix Design & Interpretation

| Scenario | Expected Result if H-A | Expected Result if H-B | Result Indicates |
|----------|---|---|---|
| **Debug Android S23** | ✓ Works | ✓ Works | Baseline (both mechanisms inactive) |
| **Release Android Emulator** | ✗ Fails (trimmed) | ✓ Works | **H-A** if fails; H-B if works |
| **Release Android S23** | ✗ Fails (trimmed) | ✗ Fails (IME) | Known failure; can't disambiguate |
| **Debug Android S23** | ✓ Works | ✓ Works | Should work; if fails → environment issue |

### Recommended Test Order

1. **Run Debug on S23** — confirm autocomplete works (baseline)
2. **Run Release on emulator** — **KEY DISCRIMINATOR**
   - If fails → H-A (trimming) confirmed
   - If works → H-B (device IME) confirmed
3. **Retest Release on S23** — validate fix effectiveness

### Interpretation Rules

- **Release-emulator fails, Release-S23 fails** → H-A (trimming everywhere)
- **Release-emulator works, Release-S23 fails** → H-B (device-specific IME)
- **Both fail** → Could indicate a third mechanism or combination
- **Both work** → Issue may have been transient or environment-dependent

---

## 5. Root Cause Confidence Assessment

### H-A (Release Trimming) — **CONFIDENCE: 85%**

**Evidence supporting H-A:**
- ✓ Trimming is active by default in MAUI 10 Release/Android
- ✓ `AutocompleteMobileField` has zero static XAML references (linker can't prove it's needed)
- ✓ Symptomatic: Works on Debug (no trimming) but fails on Release (trimmed)
- ✓ Dynamic instantiation + runtime reflection binding is classic trimming victim
- ✓ Property binding via `SetBinding()` depends on reflection to resolve property names — trimmed types/properties break this path

**Why it's most likely:**
The component architecture uses a pattern that is known to be fragile under trimming: dynamic instantiation of a type that has no static reference, followed by reflection-based property binding.

### H-B (Device IME Composition) — **CONFIDENCE: 15%**

**Evidence against H-B:**
- ✗ If IME committed text per composition-end (not per keystroke), Debug should also fail → but it works
- ✗ Debouncer correctly handles async delays
- ✗ No intermediate layers that would filter keystroke events
- ✗ IME composition typically affects keyboard event timing, not completeness of the search — would likely show partial results, not zero

**Why it's unlikely:**
The symptom (zero suggestions on Release only, works on Debug) is a classic fingerprint of trimming, not device-specific IME behavior.

---

## 6. Recommended Phase 1 Fix Approach

Based on H-A confidence (85%), the fix should address trimming robustness:

**Option 1: Preserve via Explicit Metadata**
- Add `TrimmerRootAssembly` attribute to the assembly or use a Trimmer directives file
- Explicitly mark `AutocompleteMobileField` as preserved

**Option 2: Rename & Restructure** (if Option 1 proves insufficient)
- Move `AutocompleteMobileField` creation to a static factory method
- Reference the factory in a way the linker can trace
- Alternative: Pre-instantiate `AutocompleteMobileField` in a static context

**Option 3: Validate via Logging** (Phase 1 safety check)
- Add temporary Serilog debug probes at:
  - `AutocompleteField.xaml.cs` line 208: log before/after `new AutocompleteMobileField()`
  - `AutocompleteField.xaml.cs` lines 212-215: log binding setup success/failure
  - `AutocompleteDebouncer.cs` line 44: log callback invocation
- Deploy to Release S23, capture logs
- If "new AutocompleteMobileField()" fails → H-A confirmed; if succeeds but binding fails → H-A confirmed at binding step

---

## Summary

**Diagnosis:** **H-A (Release Trimming) — 85% confidence**

The most probable root cause is that the .NET linker, when trimming the Release build, removes or corrupts the `AutocompleteMobileField` type or its property metadata because there is no static reference to it in any XAML file. The type is only instantiated dynamically in C# code, which the linker may not recognize as a necessary usage.

**Next Step:** Run Phase 1 fix with trimming-robustness approach; validate with Release emulator test before deploying to device.
