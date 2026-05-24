# Notice Board UI Framework

Use `Wonderful World > UI > Install Welcome Board Sample` in the Unity menu.

The installer creates:

- `Assets/_Project/UI/WelcomeBoard/WelcomeBoardNoticeContent.asset`
- `Assets/_Project/UI/Prefabs/WW_NoticeBoardOverlayPanel.prefab`
- `Assets/_Project/UI/Prefabs/WW_VRSystemMenu.prefab`
- `WW_UI_System` in `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- A `NoticeBoardHotspot`, `XRSimpleInteractable`, click collider, and `WelcomeBoardPanelAnchor` on `Notice_Board` / `Welcome Board`
- A `LocalizedNoticeBoardSurface` on `Poster_back` when that child exists, so the selected language poster is visible on the board before the popup opens

For another area, create a new `Localized Notice Board Content` asset, assign one sprite per language, then add `NoticeBoardHotspot` to that area's board object and point it at the shared `WW_NoticeBoardOverlayPanel`.

Language is global. Change it from the VR menu Settings page, and all open/localized notice boards update through `UILanguageService`.

Menu text is localized with `LocalizedUIText`, so the menu labels also update when the language changes.

The board popup opens with the right-hand index trigger when the right ray is pointing at the board. The popup appears in front of the player by default; enable `Use Board Anchor For Popup` on `NoticeBoardHotspot` only if you want it to open at the board anchor instead.

The menu can be opened with the left-hand controller menu button. `VRSystemMenuController` also exposes `toggleMenuAction` for a custom Input Action Reference and keeps `Esc` as an editor fallback.
