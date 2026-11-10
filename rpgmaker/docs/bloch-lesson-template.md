# Bloch Lesson Record — Template

Copy this file for each lesson record. A record missing any of the
three birth-metadata fields (`origin_lane`, `content_label`,
`evidence`) does not exist: promotion is refused, no exceptions.

```yaml
origin_lane: ""        # Where the lesson was learned, e.g. "rpgmaker", "ntranch", "qompassai"
content_label: ""      # "sfw" or "nsfw" — REQUIRED at birth; no label means the record is invalid
evidence: ""           # Pointer to the proof: file path, gate output, commit, or run artifact
```

## Lesson

- **Title:**
- **Date:**
- **Context:** What was being built or debugged when this was learned.
- **Lesson:** The transferable rule, stated once, plainly.
- **Applies to:** Engines / repos / file types the lesson covers.
- **Does not apply to:** Known limits and counter-examples.

## Lane rules (reminder)

- Sharing is governed by content label, not origin.
- Unclassified = `nsfw`, fail-closed.
- NSFW-pure lessons never reach SFW / qompassai surfaces.
- NTRanch-origin lessons transfer to qompassai only when labelled
  SFW; origin is kept on the record.
- SFW lessons may flow into NTRanch.
- `nsfw` → `sfw` reclassification is a gated Owner/Pax act only.
