# <Story name>

## Business Value
Why are we doing this, and what happens if we don't?

## Epic
Which epic in `specs/epics/` this belongs to, or "none" for a standalone
maintenance change.

## Scope
What's part of this story and what isn't? If it's too big to be Small
(INVEST), split it via SPIDR (Spike/Path/Interface/Data/Rules) first.

## As-is / To-be
- As-is: how the system behaves today.
- To-be: how it should behave.
- Gap: the minimal change that closes it. If it spans multiple layers,
  plan a walking skeleton (thinnest end-to-end path) first, then expand.

## Dependencies
What other stories, systems, or teams does this rely on, and what could
block it?

## Acceptance Criteria
- Given ..., when ..., then ...
- Given ..., when ..., then ...

## Test Plan
Map each acceptance criterion to a specific test.

## Deploy & Monitoring
When is this going live? What should ops watch for after? Tell ops this
before the deploy, not after.
