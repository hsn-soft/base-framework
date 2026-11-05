# ADR-0132: Claude works on its own branch cut from `dev` and never opens a pull request into `dev` or merges into it

- **Status:** Accepted
- **Date:** 2026-11-03 (supersedes [ADR-0025](0025-the-rewrite-lives-on-feat-new-framework.md))

## Context

[ADR-0025](0025-the-rewrite-lives-on-feat-new-framework.md) kept the whole rewrite on one branch, `feat/new-framework`, and said it is not merged into `dev` until the whole framework exists. That is not how the repository works any more: the maintainer merges it into `dev` through pull requests (#386, #388), and what reaches
NuGet is decided by the release branches (`rel/nuget-candidate`, `rel/nuget-regular`, [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [ADR-0116](0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md), [ADR-0130](0130-nuget-packages-are-pushed-with-trusted-publishing-not-an-api-key.md)), so
an unfinished `dev` cannot publish. What the rule was meant to say is **who may merge**: the assistant's work reaches `dev` only through the maintainer. The text as written made the assistant refuse work the maintainer asks for, and read the maintainer's own merges as mistakes.

## Decision

- **Claude works on a branch of its own**, `feat/<topic>` (or `fix/`, `docs/`), **cut from `dev`**. It may commit to it and push it to `origin` (the hooks run, [ADR-0013](0013-git-hooks.md)).
- **Claude never opens a pull request into `dev` or into a release branch, never merges into them (locally, with a tool, or on the web) and never pushes to `dev` or `rel/*`.** The maintainer opens and merges the pull requests, and the releases.
- To take what is in `dev`, Claude merges or rebases `dev` **into its own branch**; never the other way.
- **Branches are not stacked:** a branch is cut from `dev`, not from another branch of Claude's. Work that needs another branch that is not merged yet is a question for the maintainer.
- A branch that already exists, such as `feat/new-framework`, is continued until the maintainer asks for a new one.
- When the maintainer says that a push stays with them, Claude commits and does not push.

## Consequences

- The root `CLAUDE.md`, the git rule and the release skill say this; the open item "a draft pull request from `feat/new-framework`" of the catalog is dropped, since Claude does not open pull requests.
- ADR-0025 is superseded as a whole: its single long-lived branch and its ban on merging into `dev` no longer apply; its ban on stacked branches is kept above.
- `.claude/settings.json` denies the commands that would break it (`gh pr create` and `gh pr merge`, switching to `dev` or a release branch, pushing to them), so the rule does not rest on the text alone.
- The CI still runs on pull requests into `dev` ([ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md)): a push to Claude's branch runs nothing until the maintainer opens the pull request, so the detector runs locally first ([ADR-0014](0014-detector-before-a-pull-request.md)).

## Related

[ADR-0025](0025-the-rewrite-lives-on-feat-new-framework.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0014](0014-detector-before-a-pull-request.md), [ADR-0115](0115-versions-and-releases-two-branches-and-a-version-made-from-nuget.md), [CONTRIBUTING.md](../../CONTRIBUTING.md).
