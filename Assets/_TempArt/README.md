# _TempArt — 原神风格资源挑选区

临时目录，已在 `.gitignore`，不入库。最后由我清理 + 迁移到 `_Project/Art/Buildings/`。

## ✅ 已就绪

**1. ChiliMilk URP Toon Shader** — 已集成到项目

- 位置：[Assets/_Project/Art/Shaders/Genshin/ChiliMilkToonShader/](../_Project/Art/Shaders/Genshin/ChiliMilkToonShader/)
- License: **MIT** ([Copyright (c) 2020 ChiliMilk](../_Project/Art/Shaders/Genshin/LICENSE_ChiliMilk.txt))
- 来源: [github.com/ChiliMilk/URP_Toon](https://github.com/ChiliMilk/URP_Toon)
- 特性: Ramp Map / SDF Face Shadow / Specular / Rim Light / **Outline Pass** / SSAO

**2. 临时目录** — 按风格分类，等你下载完往里扔：

```
_TempArt/
  Liyue_Style_Candidates/         ← 璃月风（中式奇幻）
  Inazuma_Style_Candidates/       ← 稻妻风（日式）
  Skyline_Silhouette_Candidates/  ← 远景剪影（高 poly 也可用）
```

---

## 🚨 Sketchfab 下载流程（必读）

我**无法替你下载** Sketchfab 资源 — 必须你本人登录账号才能下。流程：

1. 注册免费 Sketchfab 账号（Google 一键登录最快）
2. 点开下面的链接 → 页面右上 **Download 3D Model** → 选 **Autoconverted (.fbx)** 或 **Original (Blender/glb)**
3. 解压后**整个文件夹**拖到对应的 `_TempArt/*_Candidates/` 子目录
4. Unity 会自动 import

**Attribution 义务**：CC-BY 要求**在游戏 credits 里署名**。下载后建议每个模型旁边放一个 `CREDIT.txt` 写明作者名 + Sketchfab 链接。最终我整理时会汇总。

---

## 📦 精选 Sketchfab 模型（已验证 license + 用途）

### 璃月风 (Liyue / 中式奇幻) — 用在 LotusPond / CherryGarden

| 模型 | 作者 | License | 三角面 | 用途 |
|---|---|---|---|---|
| [Chinese Pagoda Low Poly](https://sketchfab.com/3d-models/chinese-pagoda-low-poly-db941b806e4b4c8995e729fe0d4d28c4) | Niko313 | CC-BY 4.0 | 27.2k | **重点 ⭐** — 完美 VR-friendly 中式宝塔 |
| [Chinese Temple – Sacred Peak Shrine](https://sketchfab.com/3d-models/chinese-temple-sacred-peak-shrine-fe474ef5e0ee4e8d808f6a65516da008) | ARTEL_3D | CC-BY 4.0 | 498k | **天际线远景剪影** — 作者明说 "Ghibli-like, optimized for cel-shading"，但 poly 高，**只能放在视距外做剪影** |
| [Chinese Pavilion](https://sketchfab.com/3d-models/chinese-pavilion-b0f6c1fb43e744ec876faecaba3d4925) | shineSUU | CC-BY | 中 | 中式凉亭，可在 LotusPond 边上 |
| [Low Poly Chinese City](https://sketchfab.com/3d-models/low-poly-chinese-city-faf480ecd57d4c8e9a2836f5c02c4d00) | smooth998 | CC-BY | 低 | 整片中式房子聚落，背景民居 |
| [Traditional Chinese Siheyuan](https://sketchfab.com/3d-models/traditional-chinese-siheyuan-courtyard-a18881525cfd4fe882e739c9c7cee752) | andertan | CC-BY | 中 | 四合院 — 复杂结构可拆分用 |

### 稻妻风 (Inazuma / 日式) — 用在 CherryGarden / 软边界

| 模型 | 作者 | License | 三角面 | 用途 |
|---|---|---|---|---|
| [Japanese Torii Low Poly](https://sketchfab.com/3d-models/japanese-torii-low-poly-ba79bbd7ce454c92aa6c0dd9809093d8) | André Bernardo | 待确认 (页面是 free) | 3.6k | **重点 ⭐** — 严岛神社灵感，VR 极友好 |
| [Low Poly Japanese Torii (floating island)](https://sketchfab.com/3d-models/low-poly-japanese-torii-6ee6c870abb840d98e8dbca9f5b037a8) | Ktarsis | 待确认 | 中 | 浮岛 + 鸟居 — **天际线绝佳** |
| [Stylized Stone Torii + Toro](https://sketchfab.com/3d-models/stylized-stone-torii-japanese-gate-and-toro-f498017b69474621957723e524b8c0b7) | Undertaker | 待确认 | 4k | 鸟居 + 石灯笼组合 |
| [Japanese Shrine](https://sketchfab.com/3d-models/japanese-shrine-98c7ce82c65749c69ad6268a66fc2189) | Pinnacle CG Arts | 待确认 | 中 | 神社建筑主体 |

> "待确认" 的几个，下载前在 Sketchfab 页面上**确认 license 是 CC-BY 而不是 CC-BY-NC-ND**。NC = NonCommercial（本项目内部 demo 也 OK），ND = NoDerivatives 不能改。**优先 CC-BY 或 CC0**。

### 不在表里的怎么办

Sketchfab 自己搜索时用这两个筛选 URL，已经预设了「免费 + 可下载 + CC license」：

- 中式：[sketchfab.com/search → "chinese pavilion"](https://sketchfab.com/search?features=downloadable&q=stylized+chinese+pavilion&sort_by=-likeCount&type=models)
- 日式：[sketchfab.com/search → "japanese shrine"](https://sketchfab.com/search?features=downloadable&q=japanese+shrine+stylized&sort_by=-likeCount&type=models)

---

## 🎨 自动化：一键风格统一

**新流程（推荐）** — [StyleMatcher.cs](../_Project/Art/Editor/StyleMatcher.cs) 帮你做所有事：

### Step 0: 一次性配置（关键，否则描边不出）

1. 选中 `Assets/URPDefaultResources/Default_Forward_Renderer.asset`
2. Inspector 底部 → **Add Renderer Feature** → **Render Outline Feature**
3. 同样的事在 `URPDefaultResources/High.asset` / `Very High.asset` 也做一遍

> 不要选 `RenderFrontHairShadowMaskFeature` — 那是角色头发用的，建筑不用。

### Step 1: 一键风格统一

模型扔到 `_TempArt/*_Candidates/` 之后：

**菜单：Wonderland → Art → Style-Match Models in _TempArt**

它会自动：
- 扫描 `_TempArt` 下所有 .fbx / .glb / .gltf
- 用 **跟主场景 TFF 资源同一张 [TFF_Toon_Ramp_1A.psd](../Toon%20Fantasy%20Nature/Textures/TFF_Toon_Ramp_1A.psd)** 作为 Ramp Map
- 为每个材质生成 `<model_dir>/_Materials/<name>.mat`，shader 用 `ChiliMilk/Toon`
- 按命名规则自动匹配 albedo / normal / emission / occlusion 贴图
- 设统一的 outline / rim / specular 参数（见 [STYLE_GUIDE.md](../_Project/Art/STYLE_GUIDE.md)）
- 把模型的内嵌材质 remap 到外部 .mat — **场景里立刻看到效果**

详细参数和设计意图：[Art/STYLE_GUIDE.md](../_Project/Art/STYLE_GUIDE.md)

### Step 2 (可选): 手动微调

跑完 Style-Match 之后，每个材质就在模型旁边的 `_Materials/` 子目录。打开它调：
- Base Color (整体染色)
- Outline Width (粗细)
- Rim Color (边光色)
- Emission (灯笼/法阵发光)

下次再跑 Style-Match 也不会覆盖你的调整（脚本只设关键 baseline，不强制覆盖颜色）。

---

## 🛠️ 老流程：手动创建材质（备用）

如果 Style-Match 对某个模型不工作（贴图命名太怪），可以手动：

1. Project → 右键 → Create → Material → 取名 `M_Genshin_Stone` 之类
2. Inspector 顶部 Shader → 改成 **ChiliMilk → Toon**
3. 看到一堆参数 → 关键的几组：

| 参数组 | 推荐值 | 用途 |
|---|---|---|
| **Surface Options** → Cull Mode | Back | 标准 |
| **Default** → Base Map | 拖入模型的 albedo 贴图 | 主色 |
| **Default** → Base Color | 白色 | 不染色就放 1,1,1 |
| **Toon** → Shadow Tint Color | 深紫蓝 (推荐 #2D2752) | 暗部颜色 |
| **Toon** → Shadow Threshold | 0.5 | 明暗分界 |
| **Toon** → Shadow Smoothness | 0.05 | 越小越硬切 |
| **Toon** → Use Ramp Map | ✅ 勾上 | **原神感的灵魂** |
| **Toon** → Ramp Map | 用下面的渐变贴图（教程在第 4 节） | 多色阶 ramp |
| **Specular** → Specular Color | 略偏白 | 高光颜色 |
| **Specular** → Specular Range | 0.8 | 高光范围 |
| **Specular** → Specular Smoothness | 0.05 | 高光锐度 |
| **Rim Light** → Rim Color | 暖色 (#FFD9A0) | 边缘光 |
| **Rim Light** → Rim Range | 0.6 | 边缘宽度 |
| **Outline** → Outline Color | 接近黑 (#1A0E0E) | 描边颜色 |
| **Outline** → Outline Width | 0.5 ~ 1.5 | 描边粗细 |

### Step 3: 套到模型上

下载 Sketchfab 模型导入后：
1. 选中模型的 fbx → Inspector → Materials tab → "Extract Materials" 到 fbx 同目录
2. 把刚才那个材质拖到模型 mesh 上
3. Base Map 改成 fbx 里的 albedo texture
4. 描边出现 = 成功

---

## 🌈 Ramp Map 制作（原神风格的关键）

原神之所以是"原神"，**Ramp Map** 起 60% 作用。它是一张 256x32 的横条贴图，定义 NdotL 到颜色的映射。

### 快速方案：直接用现成 Ramp

我推荐这张作为起点（在 ChiliMilk demo 里有）：

```
左 25%: 深色（阴影）
25%~50%: 中调过渡（一两个色阶）
右 50%: 亮色（受光）
```

实际操作：
1. PS 新建 256x32 px，RGB
2. 用渐变工具：左边深紫蓝（#2D2752）→ 中段一两个 hard step → 右边接近 base color
3. 保存 PNG，Unity 里 **Wrap Mode = Clamp**, **Filter Mode = Bilinear** 或 Point（Point 更硬切）
4. 在材质里 Ramp Map 槽位拖入

### 偷懒方案

ChiliMilk 项目里 `Assets/UnityChan/Textures/` 有现成 ramp 可参考，照葫芦画瓢。

---

## 📍 摆放建议（按 Region）

| Region | 摆什么 | 数量 |
|---|---|---|
| **LotusPond** | Chinese Pavilion (亭子立水中) + 石灯笼 | 1 大 + 4-6 小 |
| **CherryGarden** | Torii 鸟居（樱花树下）+ 石灯笼 | 1-2 个 |
| **天际线** | Sacred Peak Shrine 远处剪影 / 浮岛鸟居 / 中式宝塔 | 2-3 个，距离 100m+ |
| **HumanEntry** | 中式牌坊（找一个 paifang 模型）或 Torii 当入口 | 1 大门 |
| **FireworksClearing** | 宝塔（Chinese Pagoda Low Poly）作钟塔焦点 | 1 高塔 |
| **软边界** | 中式月洞门、藤架（复用 [StylizedVinePergola](../_Project/Art/Props/StylizedVinePergola/)）、长廊 | 沿 perimeter |

---

## 工作流总览

```
1. 你 → 注册 Sketchfab 账号 → 下载模型 → 拖到对应 _TempArt/*_Candidates/
2. 你 → Unity 自动 import → 模型默认 Standard / URP-Lit shader
3. 你 → 创建 M_Genshin_* 材质用 ChiliMilk/Toon shader → 套到模型上
4. 你 → 拖到 World_WonderlandPark_M3_YuFu.unity 场景里对应 Region
5. 你 → 调位置 / 缩放 / 旋转 → 反复试到满意
6. 你 → 告诉我"放好了"
7. 我 → 迁移用到的资源到 _Project/Art/Buildings/<Region>/
8. 我 → 修复场景引用 + 删 _TempArt + commit
```

---

## 风险声明 (留档)

- 所有 Sketchfab 资源使用 **CC-BY 4.0** 协议时**必须**在最终游戏 credits 里**署名作者 + 链接**
- ChiliMilk Toon Shader (MIT) **必须**在游戏 credits 或 LICENSES 文件里保留 `Copyright (c) 2020 ChiliMilk`
- 本项目目前定位"内部 demo / 学校作业 / 不会商发"，CC-BY-NC（非商业）资源**也允许**使用，但**若后期改为商发必须替换 NC 资源**
- 二创原神 IP 资源 **不可用**（米哈游 IP 保护严格）
