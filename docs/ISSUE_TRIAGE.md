# Issue triage and prepared tracking issue

Reviewed: 2026-10-15. Read all nine existing issues and their comments: seven open (#3, #4, #5, #6, #8, #9, #10), two closed (#1, #2). No issue was closed or otherwise modified.

Publication status: **not created**. The GitHub connector rejected issue creation with HTTP 403, `Resource not accessible by integration`; GitHub CLI is unavailable, and the available browser session redirects to GitHub sign-in. This is a connector/account access limitation, not a missing user authorization. The exact prepared title and body follow.

[Open a new issue with the prepared title](https://github.com/Frenqy/InstancedLoot_continuation/issues/new?title=Tracking%3A%20multiplayer%20price%2C%20instancing%2C%20and%20Hallowed%20Concepts%20compatibility%20reports) and paste the body below.

Title: **Tracking: multiplayer price, instancing, and Hallowed Concepts compatibility reports**

---

## Scope and evidence

Triage of all seven existing open issues (#3, #4, #5, #6, #8, #9, #10), their comments, and the two closed issues (#1, #2), reviewed on 2026-10-15. This tracks reports and acceptance criteria; it does not establish that any bug is fixed. Keep the original reports open while investigating.

The price reports below repeat a symptom. No common root cause has been demonstrated for the wider price, overlap, artifact, and pickup failures.

## Multiplayer regressions

- [ ] **Client purchase prices / scaling:** #3, #5, and the first report in #6. #5 reports single-player prices for clients versus multiplayer prices for the host with “InstanceItem” and `InstanceBoth`; [its follow-up](https://github.com/Frenqy/InstancedLoot_continuation/issues/5#issuecomment-2677306964) and #6 describe client prices staying at stage-one values. Check the actual config (`InstanceItems` is the mode name), stage scaling, displayed price, affordability, and server-side amount charged. Verify host and client agree with the intended source/copy price on stage one and later stages.
- [ ] **Overlapping copies / blocked client interaction:** [the comment on #3](https://github.com/Frenqy/InstancedLoot_continuation/issues/3#issuecomment-2611376276) reports desync followed by overlap after a stage transition and supplies host/client logs and screenshots; #6 names Sky Meadow and Helminth Hatchery. #8 reports overlap and host-only interaction specifically with Swarms. These are related symptoms, not proven duplicates of the price bug or of one another. Verify each player can target and use only their eligible copy after stage transitions and delayed object spawning; check display separately from interaction permissions.
- [ ] **Swarms isolation:** #8 says their artifact comparison narrowed chest failure to Swarms. Repeat with no artifacts and Swarms alone before attributing it to artifact-specific spawning. Expected copies and eligibility must remain correct; no extra usable rewards.
- [ ] **Item eligibility and Command/Sacrifice:** #6 broadly reports item instancing failing. #9 reports a publicly usable Command essence followed by one public item while Command, Sacrifice, and Swarms were all active. Test ordinary drops first, then each artifact separately, Command+Sacrifice, and all three. Test both `SharePickupPickers` settings and relevant source modes: each eligible player should receive one allowed grant/choice; already-consumed and ineligible players must be blocked. A single visible pickup/essence can correctly serve several players, so visible object count alone is not a failure.

## Independent support and compatibility reports

- [ ] **Green multishops:** #4 says one purchase prevents others choosing. Current documented baseline excludes `TripleShopLarge` from handler registration; confirm the reported prefab and config before equating that gap with the observed failure. Verify each intended owner retains an independent choice and copied terminals preserve items, price, and availability.
- [ ] **Special-stage chests:** #6 reports host-only chests on Gilded Coast and Prime Meridian. Capture prefab/SpawnCard names and confirm recognition plus source context on each stage; verify the configured instancing policy permits the intended players to interact.
- [ ] **Hallowed Concepts update:** #10 is a compatibility request without logs, game build, or a concrete failing hook. Record exact game/DLC and dependency versions, check startup and hook installation, then run the multiplayer cases above before claiming support.
- [ ] **HUDdleUP counts:** #6 reports counts including other players' copies. Reproduce with a minimal integration profile and compare per-player usable counts.
- [ ] **StageRecap counts:** #6 explicitly marks this as uncertain. Verify independently before treating it as a confirmed compatibility bug.
- [ ] **Dropped-item integration:** #6 requests compatibility with an unspecified drop-item mod; no demonstrated reproduction is provided. Identify the mod/version and define whether voluntarily dropped inventory should become public before implementing an integration.
- [ ] **Forfeit shortcut:** #6 requests ping plus a configurable key instead of chat commands. Track as a usability feature, independently of synchronization failures.

## Historical issues kept separate

#2 requests reducing every player's money when one player buys in `InstanceItems`. [The original author checked the source](https://github.com/Frenqy/InstancedLoot_continuation/issues/2#issuecomment-2561484774) and states InstancedLoot never implemented shared purchase deductions. This closed feature discussion is distinct from mismatched prices in #3/#5/#6; fixing price synchronization must not silently introduce team-wide charges.

#1 is a closed repository/README maintenance discussion, not evidence of a current gameplay regression.

## Reproduction and completion standard

Use Release builds with the same mod version installed on host and client. Record game build/DLC, dependency versions, both config files (configuration is not synchronized), player count, stage, source prefab, artifacts, exact steps, and both BepInEx logs. Start with the mod plus required dependencies, then add suspected integrations. Compare `InstanceItems`, `InstanceObject`, and `InstanceBoth` where the source supports them; include stage transitions, delayed spawns/late joining, and per-camera split-screen display where applicable.

Record source-level fixes, build results, and actual in-game results separately. A successful build or corrected static code is insufficient to tick a runtime acceptance item. Original reports remain open until their own scenarios are verified.
