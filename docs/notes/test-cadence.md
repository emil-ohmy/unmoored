# Test cadence

While iterating and for commits on a feature branch: rebuild only the scene being worked on (its Build Lab menu
item, or its MainSceneBuilder method headless) and run its tests (`-testFilter <TheirClass>`), plus the tests of any
shared code touched. The full build of every scene and the whole test suite
run once, before merging to main.

**Why:** the user wants to iterate more quickly (2026-11-02); a full build and test run before every commit was too
slow. They agreed: "on a merge we check the tests". CLAUDE.md also says test-all before a merge.

**How to apply:** don't run the full suite before ordinary commits; do run it (and fix what fails) before any merge
to main, and say so in the merge report.
