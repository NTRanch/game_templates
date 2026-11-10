# Bloch Lesson Template

Copy this file for every new lesson record. A lesson is born with all
three birth-metadata fields filled — `origin_lane`, `content_label`,
and `evidence`. A record missing any of them is not promoted and not
shared (see the Bloch lane law in `SKILLS.md`).

- **title:** <!-- short, specific: what was learned -->
- **date:** <!-- ISO date, YYYY-MM-DD -->
- **origin_lane:** <!-- where the lesson was learned, e.g. ntranch / qompassai, plus the project -->
- **content_label:** <!-- sfw or nsfw. If unsure, write nsfw: unclassified defaults to nsfw, fail-closed. -->
- **evidence:** <!-- the run artifact, log, file, or measurement the claim rests on. A claim with no citable evidence does not travel. -->
- **claim:** <!-- the one-sentence claim this lesson makes -->

## Lesson

<!-- The lesson itself: what happened, what the evidence shows, and
     what to do differently (or repeat) next time. Keep it to what the
     evidence supports. -->

Guidance:

- `content_label` is governed by content, not by which lane produced
  the lesson.
- NSFW-pure lessons never reach SFW / qompassai surfaces.
- SFW lessons may flow into NTRanch; NTRanch-origin lessons reach
  qompassai surfaces only when labelled SFW, origin kept.
- Reclassification (nsfw → sfw) is a gated Owner / Pax act. Propose it
  on the record; do not edit the label yourself.
