<!--
Sync Impact Report
Version change: none → 1.0.0
Modified principles:
- Added: Small Project Scope
- Added: Clean Code First
- Added: Minimal Architecture
- Added: No Automated Tests
- Added: Documented Simplicity
Added sections:
- Additional Constraints
- Development Workflow
Templates updated:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
Follow-up TODOs: none
-->
# Docs2Pdf Constitution

## Core Principles

### I. Small Project Scope
The project MUST remain intentionally small: a single, focused deliverable with minimal dependencies
and no unneeded subsystems. Feature scope MUST be limited to the essential path, and scope expansion
MUST be evaluated as separate work.

### II. Clean Code First
All code MUST be readable, expressive, and easy to navigate. Naming and structure MUST favor clarity over
cleverness, functions and modules MUST do one thing, and complex logic MUST be split into smaller, explicit
units.

### III. Minimal Architecture
Design decisions MUST prioritize direct, simple implementations over frameworks, abstractions, and
indirection. Architecture MUST emerge from the smallest working solution, not from large upfront design.

### IV. No Automated Tests
This project MUST not include an automated test suite. Quality and correctness MUST be assured through
manual verification, examples, code review, and lightweight documentation rather than automated testing.

### V. Documented Simplicity
Code and design choices MUST be documented with concise explanations and usage examples. Documentation MUST
explain the simplest path to use or extend the project without requiring additional hidden knowledge.

## Additional Constraints
- The implementation MUST stay small and maintainable: avoid multi-package or multi-repo structure.
- External dependencies MUST be minimal and justified by direct project needs.
- Automated tests are excluded from the baseline project workflow; manual verification is the expected quality gate.
- Keep the runtime footprint lean and limit the number of files to what is needed for a small, clean implementation.

## Development Workflow
- Start each change with a clearly scoped requirement and the smallest workable implementation.
- Implement in small increments, then perform manual validation and code review before expanding scope.
- Prefer direct command-line examples, sample inputs, or explicit manual checks over automated test artifacts.
- Any added complexity MUST include a short rationale and be removed if it does not clearly improve clarity or maintainability.

## Governance
This constitution is the authoritative guide for project decisions. All development choices MUST align with
these principles. Amendments require an explicit update to this constitution, a brief rationale, and a review
by the project owner or maintainer.

Every implementation change MUST state which core principle it supports and how it preserves project simplicity.
When a principle appears at risk, the change MUST be simplified or deferred.

**Version**: 1.0.0 | **Ratified**: 2026-05-23 | **Last Amended**: 2026-05-23
