According to UX research and usability best practices (backed by organizations like the Nielsen Norman Group and the Baymard Institute), validating **when the entry loses focus (on blur)** is the clear winner for standard form fields.

Here is a breakdown of why "on blur" is the standard, why the alternatives fail, and how to apply the most sophisticated validation strategies to your forms.

## Goal

Here you find definition for when and how validation app's form entries based on the user's and form's context. They must be registered in MyVocaList claude code internal tools over any existing guideline already defined - if exists, so that stabilishing the app's pattern for coding them across every forms. 

**Crucial**: use proper UX Skill available in the project for validating this document requirements and ensure compliance to its guides.

### The Standard Rule: Validate on Blur

When a user tabs or clicks out of an input field, they are naturally signaling that they have finished their thought and completed that specific task.

**Why it works:** Validating on blur provides just-in-time feedback. It allows the user to immediately fix a mistake while the context is still fresh in their working memory, entirely avoiding the frustration of having to scroll back up to hunt for errors later.

### The Flaws of the Alternatives

**Validating on Submit (The "Wall of Red"):** Waiting until the user hits the save/submit button is considered a highly frustrating anti-pattern. If a user fills out a multi-field form and hits submit only to be bounced back to the top with a list of errors, they are forced to re-engage with fields they had already mentally checked off. This creates a high cognitive load and increases form abandonment.
**Validating on Keystroke (The "Impatient Teacher"):** Validating every time a key is pressed is equally jarring. If a field requires a 10-digit phone number and you flash a red "Invalid length" error the moment the user types the area code, you are penalizing them for a task they haven't had the chance to finish yet. It feels aggressive, premature, and distracting.

### The "Gold Standard" UX: Punish Late, Reward Early

The most sophisticated approach to form validation doesn't rely on just one event; it dynamically switches based on the user's state:

1. **Initial Input:** Validate **on blur**. Let the user finish typing in peace.
2. **Error Correction:** If a field enters an error state (e.g., they typed an invalid email and clicked away), switch to validating **on keystroke**. The exact moment they type the missing `@` or `.com`, the red error message should disappear. Don't force them to click away a second time just to find out if they fixed it correctly.

### Exceptions to the Rule

There are a few specific scenarios where deviating from the "on blur" default is actually the better choice:

* **When to use Keystroke:** Use this for inputs that require real-time guidance, such as a **password strength meter**, a **character limit counter**, or checking **username availability**. (For network requests, ensure you use a "debounce"—a slight delay of ~500ms after they stop typing—so you aren't spamming your server on every single letter).
* **When to use Submit:** Final cross-field validations (e.g., verifying that a "Confirm Password" field matches the original password, or checking inventory limits) and heavy server-side authentications are often best reserved for the final submit action. Submit validation should act as a final safety net, not your primary line of communication with the user.

## Validation Types 

**Mandatory**: all types must use proper automatic mask. E.g. Date must be masked with "/" automaticaly, and never be stored. Masks are not something persisted in database, but automatically applyied in the UI. A data stored in the DB date type must be automatically formated in the UI with / (MM/dd/yyyy) and must not be under user's manipulation beyond the month number, day number and year number.

**Caution**: App is intended to have 6 different tongues available. Among them we have Japanese that perhaps could be distinct from the USA/Brazilian pattern. Be aware of this.

### Dates 
Most of the users expect date standard to be MM/dd/yyyy - if user is an English speaker. For Brazilian Portuguese speaker, dd/MM/yyyy is expected (future - multi-language). 
 **Special cases** - MyVocaList have a special case where date is typed without the year. 
 **Crucial** - manualy typed date must validate the month, year and day numbers properly. 
 **Avoid** - reinvent the whell. validate month, year and day is already well implemented in components specialized on it. Better be reused over manualy code a custom validator. 

### Integer
...
<TODO> - complete Integer and append any 