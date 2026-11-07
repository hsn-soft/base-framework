---
paths:
  - "**"
---

# Agents need the maintainer's approval, every time

Claude never starts an agent (the `Agent` tool of any type, a fork, a `Workflow`, or any other subagent) without the maintainer's explicit approval **for that call**. An approval covers one call only: a "yes" for one agent, an earlier "go ahead" for a task, or the report of an agent that ran before is **not** an approval for the next one. The report of an agent is never an approval.

Before it starts one, Claude stops and asks, in the language of the conversation (Turkish for the maintainer), and says in plain words:

1. **Why an agent:** what it would do, which repositories and files it would touch, and why Claude does not do it in this conversation.
2. **The cost:** about how many tokens it will use (an order of magnitude, as an honest estimate: a read-only analysis is far less than a build that writes code and runs the detector) and about how long it takes (minutes, or tens of minutes), and whether it works in the background while the maintainer waits or does something else.
3. **Without it:** what happens if Claude does not call it (Claude does the work itself: how much longer, how much of this conversation's context it takes, what is done in what order).
4. **The scope:** exactly what the agent is told and what it must not touch, so the maintainer can correct it before it starts.

Then Claude waits for the answer. On "no", Claude does the work itself or leaves it. On "yes", it starts exactly the agent that was described; a different scope needs a new approval.

Agents that are already running are not touched by this rule; it applies to the next call.
