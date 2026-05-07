# S6.2 — Automated Hooks

**Status:** Researched  
**Predecessor(s) ID:** S6

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content researched and written |

---

## Overview

Automated hooks are shell scripts or code that execute at defined points in an AI agent's lifecycle, **outside the context window**. Their critical property is structural: a shell process cannot be overridden by a language model. The model cannot argue with, forget, or reason around an exit code. This relocates enforcement from a prompt-compliance problem to an architectural one.

Hooks are the operational realization of the constitutional constraints described in S6.1. Where constitutional rules state a principle ("specs must be approved before implementation"), hooks execute it mechanically. They are the boundary between guidance and guarantee.

---

## Hook Lifecycle Events

Claude Code (as of March 2026) supports 21 distinct lifecycle events where hooks can fire. The most widely used are:

| Event | Timing | Can Block? | Primary Use |
|-------|--------|-----------|-------------|
| **SessionStart** | When a session begins or resumes | No | Initialize environment, inject context, validate project state |
| **UserPromptSubmit** | When the user submits a prompt | Yes | Audit requests, inject system context, block forbidden inputs |
| **PreToolUse** | Before any tool call executes | Yes | Block dangerous operations, enforce naming conventions, validate file paths, enforce approval gates |
| **PostToolUse** | After tool completes successfully | No | Run formatters, tests, linters; generate audit logs; inject additional context |
| **PostToolUseFailure** | After tool call fails | No | Log failures, attempt recovery, notify on critical failures |
| **PostToolBatch** | After a full batch of parallel tool calls | No | Aggregate results, run batch-wide validation, inject once per batch |
| **Stop** | When the agent is about to end its turn | Yes | Enforce completion gates — tests must pass, tasks must be marked done, specs must be updated |
| **SubagentStart** | When a subagent is spawned | No | Track nested agent usage, initialize subagent resources |
| **SubagentStop** | When a subagent finishes | Yes | Aggregate results, verify subagent output quality, block low-quality results |
| **PermissionRequest** | When the agent requests a permission | Yes | Auto-approve safe operations, deny risky ones, require confirmation |
| **ConfigChange** | When the agent attempts to modify settings | Yes | Block unauthorized configuration changes, audit all modifications |
| **Notification** | When the agent sends a notification | No | Forward to external systems, format for logging, archive event stream |
| **TeammateIdle** | When an agent team member goes idle | Yes | Prevent premature idling, require sync point before idle |
| **TaskCompleted** | When a task is marked as complete | Yes | Verify task truly completed before allowing agent to move on |
| **WorktreeCreate / WorktreeRemove** | When worktrees are created or removed | No | Track branch isolation, prevent naming conflicts, audit git state |
| **InstructionsLoaded** | When CLAUDE.md or rules files load | No | Validate constitutional syntax, audit instruction versions |

---

## Hook Implementation Patterns

### Return Mechanisms

Hooks communicate with the agent harness through:

1. **Exit codes** (most critical):
   - `0`: Success, continue normally
   - `2`: Block the operation (on `PreToolUse`, `PermissionRequest`, `Stop`, `SubagentStop`, `TaskCompleted` only)
   - Other: Non-blocking error, logged but not enforced

2. **JSON output via stdout** (optional):
   - `permissionDecision`: "allow", "deny", "ask", or "defer"
   - `permissionDecisionReason`: Explanation fed back to the model
   - `updatedInput`: Rewritten tool arguments before execution
   - `additionalContext`: Extra information injected into the model's context
   - `decision`: "block" or "continue"
   - `stopReason`: Why the agent should stop

3. **stderr** (diagnostic):
   - Message displayed to user or in logs
   - Only enforces when combined with blocking exit code

### Configuration Scopes

Claude Code supports four scopes for hook configuration, with inheritance and override semantics:

| Scope | Location | Override by User? | Use Case |
|-------|----------|-------------------|----------|
| **User** | `~/.claude/settings.json` | Yes | Personal preferences across all projects |
| **Project** | `.claude/settings.json` (git-checked) | Yes | Team conventions for a specific repository |
| **Local** | `.claude/settings.local.json` | Yes | Machine-specific overrides (not checked in) |
| **Managed** | Enterprise MDM policy | No | Organization-wide mandatory rules that users cannot disable |

**Priority:** Managed > Project > User > defaults. A managed hook cannot be weakened by user configuration.

### Hook Types (Handler Patterns)

As of March 2026, four handler types are supported:

1. **Command** — Execute shell scripts (bash on Unix, PowerShell on Windows)
   - Receives JSON input on stdin
   - Returns JSON via stdout
   - Runs synchronously and blocks agent execution
   - Must complete within timeout (default 30–60 seconds)

2. **HTTP** — POST hook input to an external service
   - Sends structured JSON to a webhook URL
   - Useful for external audit logging, analytics, third-party approval systems
   - Non-blocking; failures do not halt the agent

3. **Prompt** — Single-turn LLM evaluation
   - Sends a prompt to a language model with the hook input as context
   - Model returns `{ok: true}` or `{ok: false, reason: "..."}` decision
   - Useful for context-dependent allow/deny logic
   - More expensive than command hooks; use sparingly

4. **Agent** — Multi-turn agent with tool access
   - Spawns a full agentic subprocess to evaluate the hook condition
   - Can read files, run commands, query databases
   - Highest expressiveness; highest cost
   - Useful for complex architecture validation, security policy checking

### Matcher Patterns

Hooks fire conditionally based on a `matcher` regex that filters by:

- **Tool name** (for tool-based events like `PreToolUse`): `Bash`, `Edit`, `Write`, `Read`, `Glob`, `Grep`, `WebFetch`, `Agent`, or MCP tool names
- **Pattern matching**: `Edit|Write` (either Edit or Write), `Bash(git *)` (Bash with arguments matching the pattern), `*` or `""` (all tools)

Example: A `PreToolUse` hook with `matcher: "Edit|Write"` fires only when the agent edits or writes a file, not when it reads or runs bash commands.

---

## Real-World Hook Patterns

### 1. Pre-Write Validation (Security & Quality)

Block file writes that violate security or style policies before the agent creates them:

```bash
#!/bin/bash
# Runs as PreToolUse hook on Edit|Write tools

HOOK_INPUT=$(cat)
FILE_PATH=$(echo "$HOOK_INPUT" | jq -r '.tool_input.file_path')
CONTENT=$(echo "$HOOK_INPUT" | jq -r '.tool_input.content')

VIOLATIONS=()

# Rule 1: Detect hardcoded secrets
if echo "$CONTENT" | grep -qE 'sk-[a-zA-Z0-9]{32,}|password\s*=\s*["\'][^"\']{8,}'; then
    VIOLATIONS+=("⛔ Hardcoded API key or password detected")
fi

# Rule 2: Block writes to .env files in production context
if [[ "$FILE_PATH" == ".env" ]] && echo "$CONTENT" | grep -q "PRODUCTION"; then
    VIOLATIONS+=("⛔ Production config must not be in .env")
fi

# Rule 3: Warn about excessive debug logging
DEBUG_LOG_COUNT=$(echo "$CONTENT" | grep -c "console.log\|Debug.Write")
if [ "$DEBUG_LOG_COUNT" -gt 10 ]; then
    VIOLATIONS+=("⚠️ $DEBUG_LOG_COUNT debug statements — consider removing before commit")
fi

if [ ${#VIOLATIONS[@]} -gt 0 ]; then
    echo "$(jq -n \
        --arg decision "deny" \
        --arg reason "$(IFS=$'\n'; echo "${VIOLATIONS[*]}")" \
        '{hookSpecificOutput: {permissionDecision: $decision, permissionDecisionReason: $reason}}')"
    exit 2
fi

exit 0
```

Hook configuration:
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Edit|Write",
      "hooks": [{
        "type": "command",
        "command": "/path/to/pre-write-validator.sh",
        "timeout": 10
      }]
    }]
  }
}
```

When this hook fires and detects a violation, it exits with code 2. The agent sees the reason and must regenerate the file without the violation.

### 2. Post-Write Auto-Formatting (Developer Workflow)

Automatically format, lint, and type-check files immediately after the agent writes them:

```bash
#!/bin/bash
# Runs as PostToolUse hook on Edit|Write

FILE_PATH=$(echo "$HOOK_INPUT" | jq -r '.tool_input.file_path')

# TypeScript: run Prettier + tsc type check
if [[ "$FILE_PATH" =~ \.tsx?$ ]]; then
    npx prettier --write "$FILE_PATH" 2>&1 | head -5
    npx tsc --noEmit --project "$(dirname "$FILE_PATH")" 2>&1 | head -10
fi

# Python: run Black + mypy
if [[ "$FILE_PATH" =~ \.py$ ]]; then
    python3 -m black "$FILE_PATH" 2>&1
    python3 -m mypy "$FILE_PATH" 2>&1 | head -10
fi

exit 0
```

This hook runs **after** the file is written (cannot block), but ensures consistent formatting and type safety every time the agent commits a file.

### 3. Stop Hook — Completion Gates

Prevent the agent from finishing its turn until mandatory checks pass:

```bash
#!/bin/bash
# Runs as Stop hook — agent cannot exit until this passes

# Check if stop_hook_active — if true, we've already blocked once, exit immediately
if [ "$CLAUDE_STOP_HOOK_ACTIVE" = "true" ]; then
    exit 0
fi

# Rule 1: Tests must pass
if [ -f "Makefile" ] && grep -q "test:" Makefile; then
    if ! make test > /tmp/test-results.txt 2>&1; then
        echo "$(jq -n '{decision: "block", reason: "Tests failed:\n'$(tail -20 /tmp/test-results.txt)'"}')" >&1
        exit 2
    fi
fi

# Rule 2: No uncommitted changes with failing linter
if git status --porcelain | grep -q "^[AM]"; then
    if ! npm run lint 2>/dev/null; then
        echo "$(jq -n '{decision: "block", reason: "Linter errors in changed files"}')" >&1
        exit 2
    fi
fi

# Rule 3: tasks.md must be updated if work happened
if git diff HEAD -- "*.ts" "*.tsx" | grep -q "."; then
    if ! grep -q "- \[x\]" Docs/specs/*/tasks.md 2>/dev/null; then
        echo "$(jq -n '{decision: "block", reason: "No tasks checked off in tasks.md — mark progress before exiting"}')" >&1
        exit 2
    fi
fi

exit 0
```

Hook configuration:
```json
{
  "hooks": {
    "Stop": [{
      "hooks": [{
        "type": "command",
        "command": "/path/to/completion-gates.sh",
        "timeout": 120
      }]
    }]
  }
}
```

When this hook exits with code 2, the agent sees the reason and **must continue working** — it cannot end its turn. This is how teams enforce "tests pass before you mark done."

### 4. Architectural Boundary Enforcement (Layer Checking)

Block edits that cross module boundaries or violate layer imports:

```typescript
// Pre-write hook that parses TypeScript imports and validates layer rules
// Runs as PreToolUse on Edit|Write for .ts files

import * as fs from 'fs';

const HOOK_INPUT = JSON.parse(fs.readFileSync(0, 'utf8'));
const FILE_PATH = HOOK_INPUT.tool_input.file_path;
const CONTENT = HOOK_INPUT.tool_input.content;

// Extract file's layer from path
// Layer 1: core/, Layer 2: infra/, Layer 3: services/, etc.
const getLayer = (path: string): number | null => {
  if (path.includes('/core/')) return 1;
  if (path.includes('/infra/')) return 2;
  if (path.includes('/services/')) return 3;
  if (path.includes('/ui/')) return 4;
  return null;
};

const fileLayer = getLayer(FILE_PATH);
if (!fileLayer) {
  console.log(JSON.stringify({
    hookSpecificOutput: { permissionDecision: 'allow' }
  }));
  process.exit(0);
}

// Parse imports and check layers
const importRegex = /import .+ from ['"]([^'"]+)['"]/g;
const violations: string[] = [];

let match;
while ((match = importRegex.exec(CONTENT)) !== null) {
  const importPath = match[1];
  const importedLayer = getLayer(importPath);
  
  // Rule: Layer N can only import from layers 1..N-1 (lower layers only)
  if (importedLayer && importedLayer >= fileLayer) {
    violations.push(
      `Layer ${fileLayer} cannot import from Layer ${importedLayer} (${importPath})`
    );
  }
}

if (violations.length > 0) {
  console.log(JSON.stringify({
    hookSpecificOutput: {
      permissionDecision: 'deny',
      permissionDecisionReason: violations.join('\n')
    }
  }));
  process.exit(2);
}

console.log(JSON.stringify({
  hookSpecificOutput: { permissionDecision: 'allow' }
}));
process.exit(0);
```

This hook teaches the agent your architecture rules through immediate feedback: whenever it attempts an illegal import, the hook blocks it, explains why, and the agent learns.

### 5. Spec-Code Alignment Gate (Approval Phase Enforcement)

Block implementation phase until spec is approved (part of the SmartScope phase-gate pattern):

```bash
#!/bin/bash
# PreToolUse hook — blocks Edit|Write during spec phase if not approved

PHASE_FILE=".claude/.approval-spec"
TASK_FILE="Docs/specs/*/tasks.md"

# If we're editing implementation files but spec is not approved, block
if [ ! -f "$PHASE_FILE" ]; then
    # Spec phase is not approved — only Read and Grep allowed
    TOOL_NAME=$(echo "$HOOK_INPUT" | jq -r '.tool_name')
    
    if [[ "$TOOL_NAME" == "Edit" || "$TOOL_NAME" == "Write" ]]; then
        echo "$(jq -n \
            '{decision: "block", reason: "Spec not approved — create spec first, get review approval, then place .approval-spec file"})')" >&2
        exit 2
    fi
fi

exit 0
```

This pattern gates phases: planning → spec review → implementation. Code cannot be written until the flag file exists, which only the review process creates.

---

## Known Limitations & Failure Modes

The Claude Code hook system, as documented in source control through 2026, has several known gaps:

### 1. Exit Code 2 Coverage Gaps

`PreToolUse` exit code 2 behavior varies by tool type:
- **Works reliably:** Bash, Edit, Glob, Grep, WebFetch
- **Inconsistent:** Write and certain MCP tools may not block consistently in all agent harnesses
- **Workaround:** Use JSON output with `hookSpecificOutput.permissionDecision: "deny"` as backup, which is more consistent than exit code 2 alone

### 2. Silent Hook Failures

Any exit code other than `0` or `2` is treated as a hook error and does **not** block:
- Missing hook dependencies (e.g., jq not installed) produce no error output
- Syntax errors in JSON output are silently ignored
- The hook execution fails silently, and the tool call proceeds
- **Remedy:** Test hooks before deploying; include health checks in SessionStart hooks

### 3. Idle Halt Instead of Feedback

Some versions of Claude Code treat exit code 2 on `Stop` hooks as "halt indefinitely" rather than "continue with feedback":
- The agent stops responding instead of continuing with the stderr message as a new prompt
- **Workaround:** Use JSON `decision: "block"` with explicit `reason` field; use `PostToolUse` for feedback instead when possible

### 4. Timeout Behavior

Hook timeouts vary:
- Default is 30–60 seconds depending on hook type
- No graceful degradation: if a hook times out, it is treated as a hard error and may block unexpectedly
- Long-running validations (full test suite, security scan) cannot be run in PreToolUse
- **Remedy:** Keep PreToolUse hooks under 5 seconds; move expensive validation to PostToolUse or CI gates

### 5. Permission Rule Precedence Ambiguity

When both hooks and permission rules apply:
- A `PreToolUse` hook returning `"allow"` does **not** override deny rules
- A hook can tighten restrictions but not loosen them past permission rules
- This creates a ceiling, not a floor: even "allow" hooks are subject to the permission system
- **Implication:** For strongest enforcement, combine hooks with managed-scope permission rules

### 6. Subagent Hook Inheritance

Subagents do not automatically inherit parent agent hooks:
- Each subagent must have the same hook configuration loaded (user, project, or managed scope)
- Inconsistent hook enforcement across subagents is a real operational risk
- **Remedy:** Always use project-scope `.claude/settings.json` for shared hooks, not user-scope

---

## Hook Scope Coverage Matrix

Not all hook events support all handler types or all decision-making capabilities:

| Event | Command | HTTP | Prompt | Agent | Can Block? | Matcher Support |
|-------|---------|------|--------|-------|-----------|-----------------|
| **SessionStart** | Yes | Yes | No | No | No | `startup`, `resume`, `clear` |
| **UserPromptSubmit** | Yes | Yes | Yes | Yes | Yes | No (fires for all) |
| **PreToolUse** | Yes | Yes | Yes | Yes | **Yes** | Tool name + args pattern |
| **PostToolUse** | Yes | Yes | No | No | No | Tool name |
| **PostToolUseFailure** | Yes | Yes | No | No | No | Tool name |
| **Stop** | Yes | Yes | Yes | Yes | **Yes** | No (fires once per turn) |
| **SubagentStop** | Yes | Yes | Yes | Yes | **Yes** | Agent name or ID |
| **PermissionRequest** | Yes | Yes | Yes | Yes | **Yes** | Tool name + args |
| **TaskCompleted** | Yes | Yes | Yes | Yes | **Yes** | Task name |

---

## Multi-Agent Coordination via Hooks

For teams running parallel subagents, hooks enable cross-agent coordination without inter-agent communication:

### Pattern: Sequential Phase Gates
```bash
# In main agent: block until all subagents report completion
PreToolUse on Agent tool:
  - Check if all subagent completion markers exist
  - Block if any are missing
  - Unblock once all markers present
```

### Pattern: Consensus Validation
```bash
# Hook runs after all subagents finish
SubagentStop hook:
  - Collect artifacts from all subagents
  - Run a verifier agent to check alignment
  - Block main agent continuation if consensus check fails
```

### Pattern: Resource Quota Enforcement
```bash
# Managed-scope hook prevents resource exhaustion
PreToolUse on Agent tool:
  - Check global agent count
  - Block if more than N parallel agents
  - Queue excessive agents instead of spawning
```

These patterns ensure that multi-agent swarms respect shared constraints without explicit inter-agent messaging.

---

## Integration with Spec-Driven Development

Hooks are the enforcement mechanism for the SDD workflow described in S3. They operationalize the governance layers:

1. **Constitutional enforcement** (S6.1): Hooks block any code that violates CLAUDE.md principles — no secrets, no design pattern violations, consistent style
2. **Phase gates** (S3.x): Hooks prevent premature transitions — implementation cannot start until spec is approved
3. **Continuous conformance** (S6.4.2): Hooks run on every tool call, ensuring drift detection is active and continuous, not periodic
4. **Validation gates** (S3.3): Hooks block PRs that don't meet Definition of Done — tests passing, tasks checked, documentation updated

In MyVocaList, the Stop hook is an example: it blocks agent exit if uncommitted changes remain. This is constitutional enforcement (Rule 3 in workflow.md: "Commit After Every Task"). The hook makes the rule mechanical instead of advisory.

---

## Scaling Hooks: Performance Considerations

At scale — hundreds of tool calls per session, dozens of parallel subagents — hook overhead compounds:

- **GitHub Copilot documentation** recommends keeping hooks under 5 seconds per invocation
- **PostToolUse hooks** are less critical (tool already executed) and can run async without blocking
- **PreToolUse hooks** are latency-critical and must be fast — keep them under 1 second if possible
- **Stop hooks** on long-running processes can timeout, blocking agent completion indefinitely

Best practices for performance:
- Prefer command hooks (fast) over prompt/agent hooks (expensive)
- Cache hook results when possible (e.g., parse git config once at SessionStart, inject into context)
- Use matchers to avoid firing hooks unnecessarily
- Parallelize independent hook handlers
- Set explicit timeouts and handle timeouts gracefully (fail open, log the failure, continue)

---

## Sources

- [Intercept and control agent behavior with hooks — Claude Code Docs](https://code.claude.com/docs/en/agent-sdk/hooks)
- [Automate workflows with hooks — Claude Code Docs](https://code.claude.com/docs/en/hooks-guide)
- [Hooks reference — Claude Code Docs](https://code.claude.com/docs/en/hooks)
- [Claude Code Hooks Complete Guide — Automate and Enforce Rules Reliably — SmartScope](https://smartscope.blog/en/generative-ai/claude/claude-code-hooks-guide/)
- [Claude Code Custom Hooks Deep Dive — Claude Lab](https://claudelab.net/en/articles/claude-code/claude-code-hooks-automation)
- [Claude Code Hooks: Automate Every Edit, Commit, and Tool Call — Morph Team](https://www.morphllm.com/claude-code-hooks)
- [Claude Code Hooks — prg.sh notes](https://prg.sh/notes/Claude-Code-Hooks)
- [About hooks — GitHub Copilot Docs](https://docs.github.com/en/copilot/concepts/agents/coding-agent/about-hooks)
- [Hooks – Codex — OpenAI Developers](https://developers.openai.com/codex/hooks/)
- [Agent Hooks: The Secret to Controlling AI Agents — htek.dev](https://htek.dev/articles/agent-hooks-controlling-ai-codebase)
- [Command Permissions Hook for Claude Code — GitHub (kaidhar/claude-code-permissions-hook)](https://github.com/kaidhar/claude-code-permissions-hook)
- [Agent Validator — Codagent-AI/agent-validator](https://github.com/Codagent-AI/agent-validator)
- [Right Hooks — npm registry](https://registry.npmjs.org/right-hooks)
- [Pre-Commit and CI Validation for AI Code: The Two-Stage Enforcement Pipeline — Bitloops Resources](https://bitloops.com/resources/governance/pre-commit-and-ci-validation-for-ai-code)
- [Agent hooks in Visual Studio Code (Preview) — VS Code Docs](https://code.visualstudio.com/docs/copilot/customization/hooks)
- [Enforcing Spec-Driven on AI Agents — SmartScope](https://smartscope.blog/en/ai-development/enforcing-spec-driven-development-claude-copilot-2025/)
- [Spec Drift: The Hidden Problem AI Can Help Fix — Kinde](https://www.kinde.com/learn/ai-for-software-engineering/ai-devops/spec-drift-the-hidden-problem-ai-can-help-fix/)
