Complete the following task according to the repository workflow and rules:

**Task:**
[WRITE THE SPECIFIC REQUEST HERE]

Required:

1. Read `AGENTS.md` first.
2. Identify the affected services, files, contracts, and databases.
3. Read the relevant canonical documents through the Documentation Router in `AGENTS.md`.
4. Inspect the actual source code before deciding on a change.
5. Inspect `git status` and do not overwrite unrelated changes.
6. Write a short plan before editing.
7. Implement the smallest complete change; do not refactor outside the task scope.
8. Follow the current architecture and the rules in `AGENTS.md`.
9. If the task affects a cross-service contract, database, REST/gRPC, or authentication, inspect all related providers, consumers, and callers.
10. Build, test, and smoke-check in proportion to the risk of the change.
11. Check documentation impact and update the affected `.md` files.
12. Do not document a feature as complete when its implementation is only partial.
13. Before finishing, inspect `git diff` and verify that it contains only intended changes.
14. Autonomously execute git commands to create a task branch, commit, and merge to dev when appropriate.

Do not introduce the following without explicit authorization:

- a new architecture pattern;
- a new package or library;
- a public contract change;
- a database ownership change;
- CQRS, DDD, RabbitMQ, MediatR, or a Clean Architecture migration.

These changes are allowed only when the task requests them directly or they have been explicitly approved in the repository workflow.

Finish with this report:

```text
PLAN
- ...

CHANGES MADE
- ...

FILES CHANGED
- ...

DOCUMENTATION UPDATED
- ...

VERIFICATION
- restore:
- build:
- tests:
- smoke check:

KNOWN ISSUES / BLOCKERS
- ...

GIT DIFF REVIEW
- ...
```
