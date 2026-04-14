# Explore The Wonderland
## M0 Closeout Checklist v1.0

- Date: March 27, 2026
- Scope: repo-side M0 completion check plus required human approvals

Current demonstration note:

- the route references in this document should be read as a suggested attraction arc for testing and communication, not a required mission order

## 1. M0 Deliverables Status

| Item | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Milestone plan exists | Done | `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md` | present in repo |
| Exact Day 30 route is defined | Done | milestone plan, execution packet | route is explicitly locked in docs |
| Branch naming and merge rules are documented | Done | `Docs/ExploreTheWonderland_M0_Execution_Packet_v1.0.md` | repo-side artifact complete |
| Issue board with owner tags exists in project notes | Done | `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md` | ready to copy into external tracker |
| Unity version is locked in project docs | Done | `ProjectSettings/ProjectVersion.txt`, execution packet | repo-side baseline exists |
| Hygiene issues are logged and triaged | Done | section 3 of this document | repo-side triage complete |
| Team approval of milestone plan | Needs Human Signoff | team review | cannot be proven by repo alone |
| Team confirmation of Day 30 route | Needs Human Signoff | team review | route is defined but still needs team confirmation |
| Team confirmation of Unity version lock | Needs Human Signoff | teammate installs / signoff | repo cannot verify each teammate |
| External issue board populated from seed | Needs Human Signoff | GitHub Projects / Jira / Trello | seed exists; external board still needs action |

## 2. Owner Action Status

| Owner Action | Status | Evidence |
| --- | --- | --- |
| Technical baseline, merge rules, version policy, build validation checklist | Done | `Docs/ExploreTheWonderland_M0_Execution_Packet_v1.0.md` |
| Systems backlog split into implementation tickets | Done | `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md` |
| Vertical-slice route and attraction order defined | Done | milestone plan and execution packet |
| Visual direction priorities and shader/material priorities defined | Done | execution packet section 5 |
| Onboarding, comfort, UX, and playtest criteria defined | Done | execution packet section 6 |

## 3. Hygiene Triage

| Issue | Impact | Decision | Status |
| --- | --- | --- | --- |
| `Docs/` was ignored by `.gitignore` | planning artifacts were not versionable | fix immediately by unignoring docs | Fixed in M0 |
| `.gitattributes` tracks `.asset`, `.unity`, `.prefab`, and `.mat` in LFS | conflicts with the PRD/TDD recommendation to keep text-serialized Unity YAML diffable | defer final change until team approval because it can affect existing repo workflow | Logged, not auto-fixed |
| Build settings still point at `Assets/Scenes/SampleScene.unity` | project still boots template content instead of production slice | accepted for M0, scheduled for M1 | Logged and accepted |
| `_Project` production folders are still almost empty | milestone planning is ahead of implementation | accepted for M0, expected before M1 starts | Logged and accepted |

## 4. Exit Criteria Status

| Exit Criterion | Status | Notes |
| --- | --- | --- |
| No ambiguity remains about what the Day 30 build contains | Done | scope and route are explicitly documented |
| All team members know their primary ownership zones | Needs Human Signoff | owner map exists, acknowledgement still needed |
| First implementation tickets are ready to start on Day 4 | Done | board seed includes M1-ready tickets with owners and repo targets |

## 5. Team Signoff Checklist

Use this section during the next team sync.

- [ ] Yu Fu confirms owner map and Unity version `6000.3.11f1`
- [ ] Xuanyuan Qin confirms owner map and Unity version `6000.3.11f1`
- [ ] Haobo Xu confirms owner map and Unity version `6000.3.11f1`
- [ ] Wenao Li confirms owner map and Unity version `6000.3.11f1`
- [ ] Tongyan Sun confirms owner map and Unity version `6000.3.11f1`
- [ ] Team confirms Day 30 route and attraction order
- [ ] Team confirms branch naming and merge rules
- [ ] External board is created from the issue board seed

## 6. M0 Completion Statement

Repo-side M0 is complete when:

- all planning and workflow artifacts exist in `Docs/`
- docs are versionable in git
- the starter ticket set is defined
- M1 can start without rewriting scope or ownership

Human-signoff M0 is complete when the checklist in section 5 is fully checked.

Current state after these updates:

- repo-side M0: complete
- human signoff M0: pending
