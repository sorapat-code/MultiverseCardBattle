# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6000.5.7f1 (Unity 6.5) |
| **Project Pinned** | 2026-10-23 |
| **Last Docs Verified** | NOT VERIFIED — official docs unreachable (`docs.unity3d.com` and `docs.unity.com` denied by the setup environment's network policy); only web-search result snippets were read |
| **Installed at pin time** | NOT DETERMINED (no Unity Editor on the setup machine; version read from `ProjectSettings/ProjectVersion.txt`) |
| **LLM Knowledge Cutoff** | Approx. May 2025; Unity training data likely covers up to ~2022 LTS (2022.3) |
| **Risk Level** | HIGH — version is beyond LLM training data |

## What is sourced so far

Taken from web-search result snippets, not from the pages themselves. Treat as
leads to confirm, not as verified reference.

- 6000.5.0f1 release date: Jun 15, 2026 — https://unity.com/kr/releases/editor/whats-new/6000.5.0f1
- `InstanceID` replaced by an 8-byte `EntityId` type; do not use `EntityId.GetHashCode()` as a dictionary key — https://docs.unity3d.com/6000.5/Documentation/Manual/UpgradeGuideUnity65.html
- Removed: `GameObject.rigidbody`, `GameObject.camera`, `Component.renderer` accessors and `GameObject.AddComponent(string)`; use `GetComponent<T>` / `AddComponent<T>` — same page
- Projects relying on uGUI only indirectly through Render Pipeline Core / URP / HDRP must add the `com.unity.ugui` package — same page
- 2D physics: a 6.3 or 6.4 project is migrated automatically to the new namespace on opening in 6.5 — same page
- Community thread on planned breaking changes: https://discussions.unity.com/t/planned-breaking-changes-in-unity-6-5-updated-2026-03-27/1694205

## NOT SOURCEABLE

- NOT SOURCEABLE — full list of 6.5 breaking changes and deprecated APIs, not read at https://docs.unity3d.com/6000.5/Documentation/Manual/UpgradeGuideUnity65.html (host denied)
- NOT SOURCEABLE — the 6000.5.7f1 patch release date and its release notes
- NOT SOURCEABLE — Input System 1.20.0 and uGUI 2.5.0 behaviour on 6000.5 (the project's installed versions)

## Project notes (read from the repo, not the web)

- Render pipeline: Built-in (no `com.unity.render-pipelines.*` package; `GraphicsSettings.m_CustomRenderPipeline` unset)
- Packages in use: `com.unity.inputsystem` 1.20.0, `com.unity.ugui` 2.5.0, `com.unity.test-framework` 1.7.0

Run `/setup-engine refresh` once the environment's Network access allows `docs.unity3d.com`
to fill `breaking-changes.md`, `deprecated-apis.md` and `current-best-practices.md`.
