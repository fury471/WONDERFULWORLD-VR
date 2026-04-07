# Explore The Wonderland
## M2 Closeout Checklist v1.0

- Date: April 2, 2026
- Scope: repo-side M2 completion template plus required human verification

## 1. M2 Deliverables Status

| Item | Status | Evidence | Notes |
| --- | --- | --- | --- |
| `ScaleManager.cs` exists and works in isolation | Pending | `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs` | fill after M2 implementation |
| `WeatherManager.cs` exists and 4 presets work in isolation | Pending | `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs` | fill after M2 implementation |
| `GrowthController.cs` exists and one route-changing growth interaction works | Pending | `Assets/_Project/Features/Growth/Runtime/GrowthController.cs` | fill after M2 implementation |
| `ParticleShapeSystem.cs` exists and 3 preset shapes work | Pending | `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs` | fill after M2 implementation |
| `LotusNoteTrigger.cs` exists and lotus interaction works | Pending | `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs` | fill after M2 implementation |
| `MountController.cs` exists and guided ride flow works safely | Pending | `Assets/_Project/Features/Mounts/Runtime/MountController.cs` | fill after M2 implementation |
| `FireworkController.cs` exists and 3 patterns trigger correctly | Pending | `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs` | fill after M2 implementation |
| First-pass ScriptableObject tuning assets exist | Pending | feature `ScriptableObjects/` folders | fill after M2 implementation |
| Each system can be demoed in isolation | Pending | sandbox scenes / tests / capture videos | requires human validation |
| No signature system hard-crashes play mode or blocks play flow | Pending | test results | requires human validation |

## 2. Owner Action Status

| Owner Action | Status | Evidence |
| --- | --- | --- |
| Xuanyuan Qin: ScaleShift, Weather, ParticleVitality, Fireworks gameplay logic | Pending | feature branches / repo targets |
| Tongyan Sun: Growth and Lotus interaction implementation | Pending | feature branches / repo targets |
| Haobo Xu: Mounts and guided route logic | Pending | feature branches / repo targets |
| Wenao Li: lighting/VFX support hooks | Pending | art and shared-world targets |
| Yu Fu: player-rig integration review and M3 handoff safety | Pending | core/XR review notes |

## 3. Known Issues And Residual Risks

| Issue | Impact | Decision | Status |
| --- | --- | --- | --- |
| Feature works only in one scene instance and not in isolation | high merge and integration risk | block M2 signoff until resolved | Open |
| Tuning data is hard-coded instead of asset-driven | difficult balancing and QA | move tuning into ScriptableObjects | Open |
| System works alone but breaks other systems when combined | M3 integration risk | log and triage before M3 | Open |

## 4. Exit Criteria Status

| Exit Criterion | Status | Notes |
| --- | --- | --- |
| each system works in isolation | Pending | requires owner demos |
| no signature system hard-crashes the build or blocks play flow | Pending | requires integrated test pass |
| systems expose tunable data through ScriptableObjects or prefabs | Pending | requires repo review |

## 5. Team Signoff Checklist

Use this section during the M2 closeout review.

- [ ] Xuanyuan Qin demonstrates ScaleShift, Weather, ParticleVitality, and Fireworks in isolation
- [ ] Tongyan Sun demonstrates Growth and Lotus in isolation
- [ ] Haobo Xu demonstrates guided cat route logic in isolation
- [ ] Wenao Li confirms first-pass visual/VFX hooks are ready for M3 integration
- [ ] Yu Fu confirms shared XR/core handoff risks are understood
- [ ] Team confirms M3 integration can begin without rewriting M2 system scope

## 6. M2 Completion Statement

Repo-side M2 is complete when:

- all required M2 runtime scripts exist in the correct feature folders
- first-pass tuning assets exist
- isolated system validation is possible from the repo and sandbox setups

Human-signoff M2 is complete when the checklist in section 5 is fully checked.

Current state after these updates:

- repo-side M2: not complete yet
- human signoff M2: pending future implementation and feature demos
