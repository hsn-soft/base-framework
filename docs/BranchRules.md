# Branch rules: which branch may be merged into which

Read by developers and by Claude. A pull request is checked by the workflow `CI Branch Rule` (`.github/workflows/ci-branch-rule.yml`) before it can be merged; the decision and the reasons are in [ADR-0144](adr/0144-the-source-of-every-pull-request-has-a-known-branch-name.md).

**How to read the table:** the row is the **source** branch of the pull request, the column is the **target**. `+` the merge is allowed, `-` it is not, `†` see the special rules below the table.

| source \ target | `dev` | `master` | `feat/*` | `hotfix/*` | `bugfix/*` | `rel/*` |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| **`dev`** | - | - | + | + | + | † |
| **`master`** | - | - | + | + | + | † |
| **`feat/*`** | + | + | + | + | + | - |
| **`hotfix/*`** | + | + | + | + | + | +† |
| **`bugfix/*`** | + | + | + | + | + | +† |
| **`rel/*`** | + | + | + | + | + | +† |

A branch with any other name (for example `wip`, `test2`, a person's name) cannot be the source of a pull request, and a pull request from another repository (a fork) is refused.

## Special rules (†): the release branches that publish or deploy

In the `rel/*` column the general rule is: from `hotfix/*`, `bugfix/*` or `rel/*`. A release branch that publishes has a rule of its own, and for these branches **the special rule replaces the general one**:

- `rel/nuget-candidate` takes **only** `dev` or `master`.
- `rel/nuget-regular` takes **only** `dev`, `master` or `rel/nuget-candidate`.

Every other `rel/*` branch (a new one included) takes what the general rule says, so it needs no change to the workflow to take a fix. A hotfix or a bugfix reaches a publishing branch through `dev` or `master`.

## Where to change it

The table, the workflow and the ADR move together: edit `ci-branch-rule.yml`, this document and the ADR in the same change (a new release branch with a special rule gets a row in the list above and a `case` in the workflow). Protection on GitHub (the maintainer): each protected branch requires the check `CI Branch Rule JOB`, otherwise a failing check does not stop the merge.
