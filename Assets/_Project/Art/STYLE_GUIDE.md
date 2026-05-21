# Wonderland Art Style Guide

> 风格基线：**三渲二（cel-shading）** + **原神 toon 视觉语言** + 与 [Toon Fantasy Nature](../../Toon%20Fantasy%20Nature) 包同根色调。

## 1. Shader 分工

| Shader | 用途 | 来源 |
|---|---|---|
| `ChiliMilk/Toon` | **建筑、prop、Sketchfab 引入的所有新模型** | MIT, 见 [LICENSE_ChiliMilk.txt](Shaders/Genshin/LICENSE_ChiliMilk.txt) |
| `Toon/TFF_CustomToon` | 植被、树、蘑菇等自然资源 | Toon Fantasy Nature 包自带 |
| `Toon/TFF_CustomToonOutline` | 同上，带描边的自然资源 | TFF |
| `Wonderland/Props/Toon Band Lit URP` | 单层老 prop（如 StylizedVinePergola）；新建议改 ChiliMilk | 项目自有 |

**规则**：任何**新放进场景的 Sketchfab / Asset Store 建筑**默认走 `ChiliMilk/Toon`。

## 2. 风格统一的两个核心决策

### 决策 A — 共享 Ramp Map

所有 `ChiliMilk/Toon` 材质都用 **`Assets/Toon Fantasy Nature/Textures/TFF_Toon_Ramp_1A.psd`** 作为 `_DiffuseRampMap`。

这跟现有 TFF 资源用同一张 ramp，光照过渡的颜色阶完全一致 → 建筑和树木站在一起不"打架"。

**实现**：[StyleMatcher.cs](Editor/StyleMatcher.cs) 已硬编码 ramp GUID，自动绑定。

### 决策 B — 统一 Outline / Rim / 镜面 参数

| 参数 | 值 | 理由 |
|---|---|---|
| `_OutlineColor` | `#1A0F0F` (深棕黑) | 比纯黑柔和，跟暖色场景调子吻合 |
| `_OutlineWidth` | `0.6` | VR 距离下既能看到又不糊脸 |
| `_RimColor` | `#FFC78C` (暖橙) | 模拟落日侧光，跟 TFF Day post-process 一致 |
| `_RimStep` / `_RimFeather` | `0.65` / `0.45` | 软边 rim，不抢戏 |
| `_SpecularHighlights` | **关闭** | 建筑表面应该哑光，不要金属感反射 |
| `_Smoothness` | `0.1` | 兜底 |
| `_SSAOStrength` | `0` | URP SSAO 没开，先关 |

灯笼 / 烟花 / 法阵这类需要发光的，单独把 `_EmissionColor` 调亮 + `_EmissionMap` 接贴图（`StyleMatcher` 会自动从源材质迁移）。

## 3. 工作流

### 一次性配置

1. **添加 Render Outline Feature**（让 ChiliMilk 描边能渲染）
   - 选 [Assets/URPDefaultResources/Default_Forward_Renderer.asset](../../URPDefaultResources/Default_Forward_Renderer.asset)
   - Inspector 底部 → **Add Renderer Feature** → **Render Outline Feature**
   - 同样的事在 `High.asset` 和 `Very High.asset` 各做一次
   - 不加这步，**描边不出**，看起来就是个普通 toon

### 每次新加模型

1. 模型扔到 `Assets/_TempArt/<Style>_Candidates/`（流程参考 [_TempArt/README.md](../../_TempArt/README.md)）
2. 菜单：**Wonderland → Art → Style-Match Models in _TempArt**
3. 看 Console 弹窗汇报创建了多少材质
4. 模型在场景里立刻是 toon look
5. 个别材质需要细调？打开 `<model_dir>/_Materials/<material_name>.mat` 调参，下次跑 `Style-Match` 也不会覆盖你的修改（脚本只设关键 baseline，颜色/贴图根据源材质推断，不强行覆盖）

> **重要**：`Style-Match` 会修改 model 的 import settings（`externalObjects`），相当于持久地把内嵌材质重映射到外部 .mat。运行一次就够。

### 调单个材质的速查

在 Inspector 里材质的 Foldout 顺序：

1. **Surface Options** — 一般不动；透明 prop 把 Surface Type 改 Transparent
2. **Base** — Albedo / Normal / Occlusion / Emission 槽位
3. **Shadow** — `ShadowType = Ramp`（已设好），可换 ramp PSD 试 1B / 1C 看哪张更和谐
4. **Specular** — Highlights 关；如果想要金箔感（金色佛像、铜钟）打开，调 SpecularStep
5. **Rim** — 暖色边光，弱光场景可以把 RimColor 调冷蓝营造月光
6. **Outline** — 描边粗细，远景物体可以加粗到 1.5 让剪影更强

## 4. 不该做的事

- ❌ **不要** 给同一个建筑同时挂 `ChiliMilk/Toon` 和 `TFF_CustomToon` 两种 shader 的子材质，会产生光影不一致
- ❌ **不要** 删 ChiliMilk shader 文件夹里任何 .hlsl include — 它们互相依赖
- ❌ **不要** 把 Outline pass 颜色调成纯白或纯亮色 — 看起来像 bug
- ❌ **不要** 在 Sketchfab 二创原神 IP 资源上用这套 shader 然后准备商发 — 不解决侵权（这是 IP 问题不是 shader 问题）

## 5. License Notes

- `ChiliMilk/Toon` shader: MIT, Copyright (c) 2020 ChiliMilk — 商业 / 修改 / 再分发都允许，要保留 LICENSE 文件
- `TFF_Toon_Ramp_1A.psd`: Toon Fantasy Nature 包内文件 — 跟着 Asset Store EULA，不能单独再分发
- Sketchfab 资源: CC-BY 4.0 — 游戏 credits 必须署名 (见各模型旁的 CREDIT.txt / license.txt)
