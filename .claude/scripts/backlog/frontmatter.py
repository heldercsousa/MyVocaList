"""Restricted YAML-frontmatter parser (stdlib only, NFR-1).

Supports exactly one shape: a `---` fenced block of flat `key: value` lines at
the very top of the file. Anything richer (nested block, `-` list, inline list,
anchor) is a FrontmatterError naming the offending key -- the generator must
never silently accept a structure it cannot round-trip.
"""

_FENCE = "---"


class FrontmatterError(Exception):
    """Raised for any unparseable or unsupported frontmatter."""

    def __init__(self, reason, path=None):
        super(FrontmatterError, self).__init__(reason)
        self.reason = reason
        self.path = path


def parse(text):
    """Return (keys_dict, body_str). Raise FrontmatterError on any problem."""
    if text is None:
        raise FrontmatterError("empty file -- no frontmatter block")
    lines = str(text).replace("\r\n", "\n").split("\n")

    idx = 0
    while idx < len(lines) and not lines[idx].strip():
        idx += 1
    if idx >= len(lines) or lines[idx].strip() != _FENCE:
        raise FrontmatterError("file does not start with a '---' frontmatter fence")

    idx += 1
    keys = {}
    while idx < len(lines):
        raw = lines[idx]
        stripped = raw.strip()
        if stripped == _FENCE:
            return (keys, "\n".join(lines[idx + 1:]))
        if not stripped or stripped.startswith("#"):
            idx += 1
            continue
        if raw[:1] in (" ", "\t"):
            raise FrontmatterError(
                "indented/nested value is not supported near: {0}".format(stripped)
            )
        if stripped.startswith("- "):
            raise FrontmatterError("list item is not supported near: {0}".format(stripped))
        if ":" not in stripped:
            raise FrontmatterError("line is not 'key: value': {0}".format(stripped))

        key, _, value = stripped.partition(":")
        key = key.strip()
        value = value.strip()
        if not key:
            raise FrontmatterError("empty key near: {0}".format(stripped))
        if key in keys:
            raise FrontmatterError("duplicate key: {0}".format(key))
        if value.startswith("[") or value.startswith("{"):
            raise FrontmatterError("inline collection is not supported for key: {0}".format(key))
        if not value:
            # A bare `key:` introduces a nested block in YAML -- reject it.
            raise FrontmatterError("key has no inline value (nested block?): {0}".format(key))
        keys[key] = _unquote(value)
        idx += 1

    raise FrontmatterError("unterminated frontmatter block (missing closing '---')")


def _unquote(value):
    """Strip one matching pair of surrounding quotes, if present."""
    if len(value) >= 2 and value[0] == value[-1] and value[0] in ("'", '"'):
        return value[1:-1]
    return value
