# Recovered Demo Restoration

Unity: 2022.3.62f3. Working project: YJ.
Restoration branch: repair/recovered-demo-2026-09-16.
The old remote main branch is not replaced.

## Completed

- Baseline: 541a893 (working recovered June demo).
- Health/equipment: 30901bf. Restored five equipment effects and their asset
  bindings, zero-damage guards, health bounds, player kill attribution,
  revival cleanup and health-bar subscription lifecycle.
- Boss stage (2026-09-17): attack cooldowns/repeat limits; committed demon
  attack selection followed by approach; wolf chase/dash tuning; timed armor
  protected against repeated stagger; one damage-audio entry point.
- Demon breath now samples baked, fire-only geometry for the current visible
  animation frame. Body, head torches, transparent gaps and smoke are excluded.
  Replacing the source animation requires rebaking the geometry.
- Collapse starts after five seconds of playable loot time, then locks input.
  Continuous shake and BGM precede the maiden by three seconds and continue
  through dialogue until teleport. Duplicate directors cannot start concurrent
  sequences. Configured scene dialogue is retained.
- Scene bindings: demon intro slider/presenter/text explicitly assigned;
  collapse camera director now resides on SceneAudio, not the destroyed boss.
  Wolf and demon UI groups verified separately.
- Lifecycle stage (2026-09-19): level1 now explicitly references the main flask
  asset and includes it in starting items. Auto-equip waits for inventory
  initialization rather than a fixed number of frames. Renaming a bound flask
  no longer breaks registration. A duplicate flask component cannot destroy
  its player/inventory host.
- Dead players cannot consume main/sub flasks; main flasks also reject full
  health, zero healing and the zero-health Moon Shadow survival window.
- Death/rest skill cancellation now removes all spawned suns and immediately
  releases their enemy control. Disabling the Sun skill also cleans up suns.
- Slow expiry no longer resumes animation/movement during an active freeze.
  Freeze expiry retains an active slow. Disabling/re-enabling enemies clears
  control state without old timers releasing newly applied freezes.

## Verification

All counts below are assertions, not separate test cases.

- RepairValidation.RunCore: 41 passed.
- DemonBossCombatValidation.Run: 1713 passed, including all 21 breath sprites,
  transform flips/rotation/scale, disabled attacks, cooldowns and repeat limits.
- BossSceneValidation.Run: 1630 passed, including scene references, missing
  scripts, UI group isolation, loot wait ordering and end-scene build inclusion.
- BossRuntimeValidation.Run: 20 passed in Play mode in an empty unsaved scene.
  Rapid hits, timed armor expiry, precise-dodge time stop, hurt audio during
  both armor modes, and zero damage were exercised on both bosses.
- LifecycleValidation.Run: 32 passed, including serialized scene/build flask
  dependencies and isolated Play-mode flask, sun and enemy-control checks.
  Fixtures do not load or save the player's progress.
- All five checks above were run again on 2026-09-19.
- dotnet build YJ.sln --no-restore: 0 warnings, 0 errors.
- Windows build (2026-09-19): succeeded, 0 errors, 8 warnings. Output:
  Builds/RepairDemo/YJ.exe (ignored by Git).
- git diff --check: passed.

Build warnings: two unused editor-only TilemapSlopeCollider fields, two
8192-pixel font-atlas warnings from the headless device's 4096 limit, and four
ambient/reflection-probe warnings because the headless renderer is null.
Fonts and lighting still need visual validation on the target graphics hardware.

## Running Checks

Use Unity batch mode with -executeMethod and the method names above.
For BossRuntimeValidation.Run and LifecycleValidation.Run, omit -quit;
each enters/exits Play mode and exits
the batch editor itself. Other checks use -quit. Do not run multiple Unity
processes against this project simultaneously.

The fire baker requires Python, Pillow and NumPy:

```text
python tools/bake_demon_fire_hitboxes.py
python tools/bake_demon_fire_hitboxes.py --check
```

Its diagnostic overlay is written to Logs/CombatValidation/fire-hitboxes.png.

## Still To Verify

- Full visual playthrough of both real boss arenas and the maiden/end transition
  in the Windows player. Compilation and isolated Play-mode checks do not
  establish complete visual parity with Editor Play.
- Remaining historical changes must be reviewed against live code individually;
  do not overwrite the project wholesale with recovered text snapshots.
- Continue auditing build-only initialization, save/rest lifecycle and remaining
  equipment/control interactions in the next restoration stage.
- See HISTORY_AUDIT.md for the partial historical audit and confirmed gaps.
  Passing these checks does not mean every historical feature is restored.
