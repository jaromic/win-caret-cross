# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Mental model: requirements drift, not a feature factory

Most work here is not "build a new feature." It's: a requirement changed — figure out the diff between how the system behaves today (as-is) and how it needs to behave (to-be), validate that the diff is real and worth closing, then make the smallest change that closes it. Treat every story this way by default, whether it reads like a bug fix, a tweak, or a "new" capability — the as-is/to-be diff is the unit of thought, not the feature.

Genuinely new capability that doesn't fit under any existing goal is the exception, not the default case to design the workflow around.

## Hierarchy

- **Goal** — a strategic outcome (e.g. "increase customer return rate by 20%"). Rare and stable; don't create or reinterpret one per unit of work.
- **Epic** — an initiative that contributes to a goal, grouping related stories (e.g. "reminder emails," "return gamification"). Created when a goal needs to be broken into a few independent bets.
- **Story** — a full vertical increment of the product that contributes to its epic (or, for a small standalone maintenance change, stands alone with no epic). This is the default unit of day-to-day work. Should satisfy INVEST — Independent, Negotiable, Valuable, Estimable, **Small**, Testable. If it isn't Small, split it before starting (see Scope, below).
- **Acceptance criteria** — testable statements attached to a story, defining what "done" means for it. These drive acceptance testing (FAT) directly.

Most work only touches the Story level, against an existing epic and goal. Only reach up to create a new Epic or Goal when the work genuinely doesn't fit under one that already exists.

## Working a story

0. **Business value** — why are we doing this and what if we don't?
1. **Scope** — what is part of the story and what is not? If it's too big to be Small, split it — by Spike, Path, Interface, Data, or Rules (SPIDR) — into slices that are each independently shippable, not half-built pieces of one feature.
2. **Implementation — as-is vs. to-be** — what's the current behavior, what should it be, and what's the minimal change that closes the gap? Write this down before coding. For anything spanning multiple layers or components, build the thinnest possible end-to-end path first (a walking skeleton), get it deployed and observed, then expand — don't build the whole to-be state in one pass.
3. **Dependencies** — what other stories, systems, or teams does this rely on, and what could block it?
4. **Acceptance criteria** — testable statements defining the story's done state, phrased as Given/When/Then so they translate directly into tests.
5. **Tests** — derived directly from the acceptance criteria (this is the acceptance test / FAT).
6. **Working software** — implement until the tests pass. Prefer shipping the walking skeleton behind a feature flag on trunk over holding everything on a branch until the whole story is done.
7. **Deploy** — ship it, and tell ops what's going live and when, *before* it ships, so monitoring is in place ahead of time rather than assembled after something breaks.

## Ops is decoupled, not a per-story loop

Ops is not "run ops on this story, get the next backlog item out." Two separate tracks happen on their own schedules:

- **Incident response** — production issues get handled immediately, as operational work, independent of any backlog. Follow the blameless postmortem pattern: incident → postmortem → action items → backlog. A fix may later spawn a story to prevent recurrence, but that's a separate, later decision — not part of closing the incident.
- **Planning feedback** — patterns from incidents, metrics, and observed behavior feed new stories and epics over time, but asynchronously. It is not a tight loop where every deploy is expected to produce the next backlog item.

## Where this lives

- `specs/goals.md` — the running list of active goals. Edited rarely.
- `specs/epics/<slug>.md` — one file per epic; see `specs/epics/TEMPLATE.md`.
- `specs/stories/<slug>.md` — one file per story; see `specs/stories/TEMPLATE.md`.

For the container/build mechanics of this repo, see `README.md`.
