# Historical Feature Audit

Snapshot: 2026-09-19, recovered YJ project.
This is a partial, evidence-based audit, not a declaration that every past
request has been restored. Later user decisions supersede earlier plans.

## Restored and Automatically Checked

| Area | Evidence / scope |
| --- | --- |
| Health bounds, zero damage, revive cleanup, overhead bar re-enable | RepairValidation.RunCore |
| Five equipment effects and asset bindings | RepairValidation.RunCore; bindings and key health/attribution cases, not every proc in a full fight |
| Both bosses: attack selection, repeat limits, armor and hurt sounds | DemonBossCombatValidation.Run; BossRuntimeValidation.Run |
| Demon fire matches current fire animation geometry | 21 baked frames plus transform/scene checks; visual playthrough still required |
| Separate boss UI groups and collapse/end references | BossSceneValidation.Run; full arena transitions still require visual QA |
| Five seconds of playable loot time before collapse lock | BossSceneValidation.Run; preserves continuous quake after the wait |
| Main flask included in the build and delayed auto-equip | LifecycleValidation.Run; explicit level1 reference and starting item |
| Death/rest removes live suns and releases enemies | LifecycleValidation.Run; SkillManager cancellation path |
| Freeze/slow overlap and disable/re-enable | LifecycleValidation.Run; real Play-mode timers |

Exact assertion counts and build results are in RESTORATION.md. An assertion
count is not a count of independent end-to-end scenarios.

## Code Present, Further Regression Needed

| Requirement | Current evidence | Remaining check |
| --- | --- | --- |
| New game resets skill unlocks to only Ember Step | MainMenuController.NewGame -> WorldRestManager.ClearGameplayProgress -> SkillManager.ResetSavedUnlocksToDefaults | Fresh/old save UI flow in the built player; do not clear the user's save merely to audit |
| Rest does not respawn bosses | WorldRestManager.ShouldExcludeFromRestRespawn excludes boss interfaces | Defeat boss, rest, return to arena in the built player |
| Stable skill identity after renaming | UI_SkillTreeSlot.skillIdOverride | Audit every live slot's serialized ID and prerequisites |
| Crafting shortages shown red; material strip excludes consumables | UI_CraftWindow.SetupCraftWindow and Inventory.UpdateSlotUI | Hover/craft/change inventory with the real UI |
| Lava/spikes affect player only | InstantDeathHazard and TilemapInstantDeathHazard player-only entry points | Both hazard types in the real level |
| Opening story, awakening UI, end slides and audio | Existing controllers/presenters in Assets/Scripts | Full new-game-to-end route and visual/audio timing |
| Package tooltip, death music, maiden dialogue and portals | Existing implementation must be checked against actual scene bindings | Do not mark restored solely because a script exists |

## Confirmed Gaps / Decisions Needed

1. Hellfire: PlayerItemDrop only has a placeholder comment reserving death
   behavior for the future Hellfire system. No functioning death-to-Hellfire
   accumulation was found. The recorded requirement is growth based on
   unspent embers at death; its formula, bounds and UI/persistence rules need
   recovering or agreeing before implementation. Do not invent those numbers.
2. Complete save: current saves cover position/health, wallet, level, skill
   unlocks and flask progress. Inventory.Start recreates item/equipment lists;
   there is no complete item/equipment/chest/boss/world snapshot. A save file
   existing does not imply those states are persisted. This is a current
   capability gap, not proof that an older implementation was lost.
3. Frost Chime weapon/effect asset is restored, but its acquisition through a
   recipe/drop/catalog still needs checking and design confirmation.
4. Editor/player parity: Windows build succeeds, but full visual playthrough
   has not been completed. Headless graphics warnings are not a visual pass.

## Next Work Order

1. Finish serialized UI/skill/portal bindings audit and the real gameplay
   regression route before adding more mechanics.
2. Define and implement a complete save schema with stable item/world IDs,
   versioning, new-game reset and old-save handling; test across scene reloads
   and process restarts without using the user's actual save as a fixture.
3. Recover/confirm Hellfire balancing and implement its death event, display
   and persistence as a separate small stage.
4. Review remaining equipment descriptions and acquisition paths one by one.

Commit each verified stage on the repair branch. Do not replace remote main,
delete recovery evidence, or copy historical scripts over live files wholesale.
