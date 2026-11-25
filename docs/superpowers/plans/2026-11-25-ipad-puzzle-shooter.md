# iPad Puzzle Shooter Implementation Plan

> Use superpowers:executing-plans in the current Orca worktree. The approved design and execution require no second approval.

**Goal** — Deliver the authorized iPad landscape gameplay prototype with a reusable board and a deterministic automatic shooter.

**Architecture** — The new scene uses the existing puzzle board and feedback with an opt-in tablet mode. A combat model owns time, palette, wave, health, shots and results. A tablet controller maps puzzle events to that model and renders managed combat state with reusable UI images.

**Tech stack** — Unity 2022.3.8f1, UGUI, existing DOTween and game assets, Unity Test Framework, built-in GPT Image.

**Spec** — [Approved design](https://github.com/luos1/RainbowThreeMatch/blob/1709e65d4afe8dec1f6fee480976d7771b5431cb/docs/ipad-shooter-design.md).

## Constraints and review focus

- Base on PR #6 (00f3ad9), which carries the original board clipping and SFX. The implementation can be reviewed without the design-only PR #7 or iOS sample fix PR #8.
- Preserve the portrait mode and authentication. The tablet scene is available via Unity scene selection in this prototype.
- Reuse MatchRules, Game, ColorHelper, MatchFeedbackPanel, MatchBurst and Sfx. No new package. Keep effects bounded and scoped to the relevant pane.
- Verify no output from invalid input, refills, or defensive groups. Verify FIFO pressure does not discard available shots. Keep an active match's palette snapshot through the unlock boundary.
- Test round cleanup with pending shots and active shield, simultaneous lethal damage and timeout, unsafe pane edges, and teardown/reentry.

## 1. Combat model and executable test cases

- [x] Add TabletCombatTests.cs with red checks for 3/5/12 player shots, power types and defensive math.
- [x] Add Assets/Scripts/Tablet/TabletCombat.cs. Expose Reset, ApplyMatch, Advance, HP, timer, palette and render state. Use fixed 60s and timed palette thresholds, scripted waves totaling 32 HP, stable target, hit effects and single-projectile multi-target logic. Verify FIFO cap, result priority, defensive cap and reset using NUnit.
- [x] Verify legacy Play Mode cases still pass.

## 2. Existing board and an isolated scene

- [x] Add TabletScene.cs/View.cs, theme asset wiring and 03.TabletPuzzleShooter.unity. Use the original 02 scene as authoring source and select the mode via a separate component. Register the new scene.
- [x] Wire Game with the optional policy at initialization, start the tablet clock after existing drop completion, snapshot the active palette before each match, and reset all state on Retry.
- [x] Fit original MatchFeedbackPanel to the left 45% safe area, show the effect and actual neighbor colors. Place status, wave and combat visuals in the right 55%; no battle input surface.
- [x] Exercise the actual scene with direct EventSystem input and controlled board/time fixtures. Verify safe bounds at 4:3 and mini, pop/notification/audio teardown, end/Retry/reentry, plus a natural elapsed run.

## 3. Generated sprites, UI verification and publishing

- [x] Create a ship atlas, effects/icon atlas, and arena background via built-in GPT Image. Save originals, prompt text, source-to-sprite selection and actual alpha checks. Import hand-selected sprites as UGUI-compatible single Sprites with centered pivots.
- [x] Capture real Game View screenshots and a clip. Record rendered dimensions and measured bounds; validate contrast/alpha on dark, white and composited backgrounds. Iterate on observed differences.
- [x] Fresh full Play Mode XML and scope review. Publish the Lore commit against PR #6. GitHub Issues is disabled for this repository.

## Verification

Unity 2022.3.8f1 compiled the prototype and passed 51 Play Mode tests. Game View used controlled puzzle and time fixtures and the actual Unity EventSystem at 1024×768 and 1133×744. Each layout passed 11 checks, and a separate 60-second run reached the survival result with eight board-triggered heals. The captured Game View clip includes the Unity audio mix. Terminal, Retry and reentry behavior are covered by Play Mode tests. Simulator and physical devices remain separate validation targets.

[Generated art and import verification](../../ipad-shooter-art.md).
