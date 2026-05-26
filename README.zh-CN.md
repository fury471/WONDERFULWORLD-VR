<p align="center">
  <img src="Docs/images/wonderland-cover.png" alt="Wonderland —— 来这里探索，尽情玩耍" width="100%">
</p>

<p align="center"><em>MAMF45 —— Virtual Reality in Theory and Practice</em></p>

# Wonderland 奇幻乐园

> *来这里探索，尽情玩耍。*

一座 PC VR 奇幻乐园 —— 第一人称、舒适的环境，有蝴蝶、莲池音乐平台、花朵魔法、动物坐骑、能亲手种植的蘑菇林、盛放的樱花树和绚烂的烟花。

**语言：** [English](README.md) · **中文** · [Svenska](README.sv.md)

---

## 项目简介

Wonderland（项目内部也称 *Wonderful World*）是一款基于 **Unity 6** + **OpenXR** + **通用渲染管线（URP）** 构建的单人 VR 探索体验。玩家在一个连贯无缝的乐园切片中漫游，七个相互连通的主题区构成一个个独立的奇妙体验，而非一条任务链。

我们的设计准则始终如一：

1. **舒适优先**。稳定的帧节奏、按模式切换的隧道式视野晕影、默认传送、缩放过程的眨眼黑屏过渡、不强加被动位移。
2. **可探索的惊喜**。每个区域都是值得被发现的独立交互，而不是必须打卡的关卡。
3. **风格化，不写实**。基于 Single Pass Instanced URP 的卡通着色（Toon Fantasy Nature）。

当前的生产场景是 [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity)，发布版本为 **v1.0.0**。

---

## 亮点

- **七个主题区域、一个连贯的乐园** —— 入口、魔幻粒子花园、莲池、动物森林区域、瀑布与烟花场地、蘑菇生长地、樱花庭。
- **三种身体尺度** —— 在 普通 / 小尺度（0.25×）/ 大尺度（1.75×）之间切换，通过右摇杆按压双击（0.32 秒内）或长按 0.45 秒触发；视高、移动速度、交互距离会通过 0.4 秒眨眼过渡自动适配。
- **莲池音律** —— 七片浮叶按 *do · re · mi · fa · sol · la · si* 自然大调音阶调音，从任意手柄发出弧形的水之魔法击中莲叶来弹奏；额外的"曲目触发器"会随机抽一首曲子让玩家跟弹。
- **动物森林坐骑系统** —— 三只独立坐骑（猫、狗、马），各自的待机踱步、悬停描边与靠近时的叫声。骑乘需要适配身体尺度：**骑乘猫和狗需要切换到"小尺度"，骑乘马则需要"普通尺度"**。马匹可通过 **左手 X** 在任意位置呼唤前来。
- **导览蝴蝶** —— 三只实时蝴蝶，玩家骑乘**猫**靠近时会沿样条飞行点起飞。
- **粉色水晶 —— 花瓣 & 花粉魔法** —— 右扳机长按对准粒子花园里的魔法水晶，粒子会沿二次贝塞尔曲线汇聚到你眼前的悬浮球；长按 3 秒后释放进入"蓄力"状态，松开后随机进入六种程序化爆发之一：`SpiralBloom`、`MathRibbon`、`TornadoVortex`、`AizawaFountain`、`DreamAttractor`、`GalaxyVeil`。
- **蘑菇种植** —— 点按种下 1 棵（土系魔法飞行 1.55 秒落地）；长按蓄力 ≥ 0.65 秒再松开，则在落点 4 米半径环形生成 5–8 棵。对已有蘑菇再次扣扳机即可"培育"使其长大（每次 +0.35×，上限 2.4×）。
- **烟花终章** —— 瞄准魔法迫击炮（识别距离 36 米），发射螺旋火带沿三次贝塞尔曲线飞向装置，触发整段点云烟花表演。
- **樱花水晶球** —— 樱花树上方 1.05 米半径的发光水晶（由 `CherryGardenCrystalOrbTrigger` 生成），右扳机激活后 0.72 秒内收束，触发四段树木生长动画与花瓣漩涡。
- **木质秋千** —— 在公园里坐上木质秋千（`TFF_Wooden_Swing_01A`，挂在 `Decorations/Swings` 下）：右手射线对准**坐板** + 右扳机即可坐下；左摇杆前/后摆动来"蹬"出更大的摆幅（围绕坐板局部 Z 轴，最大 ±60°）；右手 A 起身。视角水平锁定在坐板初始朝向，仅有平移、不带翻滚 / 俯仰。骑乘中长按右手 B（0.40 秒）即可重新归中到坐板上。**仅普通尺度可用。** 驱动脚本：[`QuestSwingRideController`](Assets/_Project/Features/Mounts/Runtime/QuestSwingRideController.cs)。
- **Quest 3 Link 舒适层** —— 专门的 `QuestLocomotionComfortProfile`、右手 B 长按 0.40 秒归中、骑乘感知的视角归中、按模式切换的隧道晕影。

---

## 技术栈

| 模块 | 工具 / 版本 |
| --- | --- |
| 引擎 | Unity `6000.3.12f1`（Unity 6） |
| 渲染管线 | Universal Render Pipeline `17.3.0` |
| 立体渲染 | Single Pass Instanced |
| XR 运行时 | OpenXR `1.16.1`（XR Management `4.5.4`） |
| 交互 | XR Interaction Toolkit `3.3.1`、XR Hands `1.7.3` |
| 输入 | Unity Input System `1.19.0` |
| 脚本后端 | IL2CPP（发布）、Mono（编辑器） |
| 目标设备 | Meta Quest 3（Link 线缆）、Windows PC VR |
| 帧节奏 | 最低 72 Hz，目标 90 Hz |

---

## 快速开始

> **务必先读这一段。** 本仓库的所有二进制美术资源（场景 `.unity`、预制体 `.prefab`、材质 `.mat`、贴图 `.png`/`.tga`/`.tif`、FBX 模型、音频 `.wav`/`.mp3`、`.asset` 等）都通过 **Git LFS** 存储。如果直接 `git clone` 而**不**启用 LFS，这些文件会变成约 100 字节的指针文本，Unity 打开后会大面积缺失资源。请严格按下面的步骤来。

### 步骤 0 —— 一次性安装前置依赖

| 内容 | 安装位置 / 方式 |
| --- | --- |
| 硬件 | Windows 10/11 PC（VR 级 GPU） + Meta Quest 3 + Link 线缆（或任何支持 Quest Link 的 USB-C 3.0+ 线缆） |
| Unity Hub | <https://unity.com/download> |
| Unity Editor `6000.3.12f1` | 在 Unity Hub → **Installs → Install Editor** 中安装。模块选择那一步，**务必勾选 `Windows Build Support (IL2CPP)`**（可同时勾选 *Documentation* 和你常用的 IDE）。 |
| Git for Windows | <https://git-scm.com/download/win> |
| Git LFS | <https://git-lfs.com/> —— 安装完后，在任意终端跑一次 `git lfs install`。 |
| Meta Quest Link 桌面端 | <https://www.meta.com/quest/setup/> |

### 步骤 1 —— 用 LFS 正确克隆仓库

在你想放置项目的目录下，打开终端（PowerShell、Git Bash 或 Windows Terminal 均可），依次运行：

```bash
git lfs install                                              # 每台机器跑一次即可；重复跑无害
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
git lfs pull                                                 # 下载所有 LFS 跟踪的二进制资源
```

预计下载量约 **2–3 GB**。`git clone` 本身很快，`git lfs pull` 是最耗时的一步。

**校验是否拉取成功。** `git lfs pull` 跑完后，主场景应当是真正的二进制文件，而不是指针：

```bash
# PowerShell
(Get-Item Assets/_Project/World/Persistent/World_WonderlandPark.unity).Length
# Git Bash / WSL
wc -c < Assets/_Project/World/Persistent/World_WonderlandPark.unity
```

正常结果是**几 MB**。如果只有几百字节，说明 LFS 没拉下来 —— 再跑一次 `git lfs pull`。

> 已经裸克隆过、没用 LFS？**不需要重新克隆**。进到目录里跑 `git lfs install && git lfs pull` 就行。

### 步骤 2 —— 在 Unity 中打开项目

1. 打开 **Unity Hub** → **Add** → **Add project from disk** → 选择 `WONDERFULWORLD-VR` 文件夹。
2. 项目卡片会显示所需的编辑器版本 `6000.3.12f1`。若未安装，Unity Hub 会引导你安装 —— 接受，并在模块列表里**务必勾选 `Windows Build Support (IL2CPP)`**。
3. 点击项目打开。首次导入会在本地从零重建 `Library/`，**通常需要 10–30 分钟**（取决于磁盘和 CPU）。**导入期间不要关闭 Unity。**
4. 导入完成后，看一下 **Console** 窗口，应当**没有编译错误**。
5. 在 **Project** 窗口里，双击 [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity) 加载生产场景。

### 步骤 3 —— 通过 Link 在 Quest 3 上运行

1. 用 Link 线缆（或任何支持 Quest Link 的 USB-C 3.0+ 线缆）把 Quest 3 接到 PC 上。
2. 在 Windows 上打开 **Meta Quest Link** 桌面端，确认头显已识别为 *Connected*。
3. 戴上头显，弹出 **"是否启用 Quest Link？"** 时点确认；或者在头显内通用菜单里 → **快捷设置 → Quest Link** 开启串流。
4. 回到 PC 上的 Unity，点击 **▶ Play**，几秒内戴上头显，XR 原点应当能追踪到头和双手。

### （可选）自己出一个 Windows 包

仓库本身**不附带**预构建的可执行文件（`Builds/` 已在 `.gitignore` 中）。要自己出一个：

1. 在 Unity 里打开 **File → Build Profiles**（或 **Build Settings**）。
2. 选择 **Windows, Mac, Linux**，Target Platform 设为 **Windows**，架构 **x86_64**。
3. 在 *Project Settings → Player* 里确认 **Scripting Backend = IL2CPP**、**Color Space = Linear**。
4. 点 **Build**，选择输出目录（推荐 `Builds/Windows/`）。

> 构建目标：**Windows / x86_64 / IL2CPP / Linear 色彩空间 / Single Pass Instanced**。

### 故障排查

| 症状 | 可能原因 | 处理 |
| --- | --- | --- |
| 大量粉红色/紫红色材质、脚本丢失、提示 "Could not extract GUID" | LFS 对象没有拉下来 | 在仓库里跑 `git lfs install`，再跑 `git lfs pull`；然后在 Unity 里右键 `Assets/` → *Reimport* |
| Unity Hub 提示编辑器版本缺失 | 没装 `6000.3.12f1` | Unity Hub → **Installs → Install Editor** 安装它，**勾选 `Windows Build Support (IL2CPP)`** |
| Quest Link 检测不到头显 | 线缆是 USB-C 2.0、头显里 Link 没开、或驱动异常 | 换 Quest Link / USB-C 3.0+ 线缆；头显内 *设置 → 系统 → Quest Link* 中启用；重启 Meta Quest Link 桌面端 |
| 首次打开就一堆编译错误 | `Library/` 不完整或来自别的机器 | 关掉 Unity，删除 `Library/`、`Temp/`、`obj/`，重新打开让它完整导入一次 |
| 头显里出现黑屏闪烁、撕裂、卡顿 | 性能/设置问题 | 按 [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md) 里的分诊流程逐项排查 |
| `git lfs pull` 很慢或卡住 | LFS 带宽或网络问题 | 重新跑 `git lfs pull`，LFS 会从断点续传 |

---

## 操作速查

### 全局

| 行为 | 输入 |
| --- | --- |
| 传送（默认） | 向前推 **左摇杆** → 松开 |
| 平滑移动（备选） | 推 **左摇杆**（`smoothMoveSpeed = 1.6 m/s`） |
| 转向：定角（默认） | **右摇杆** 左/右（`snapTurnAmount = 30°`） |
| 转向：平滑（备选） | **右摇杆** 左/右（`smoothTurnSpeed = 45°/s`） |
| 缩放：普通 ↔ 小 | **右摇杆按压双击**（0.32 秒内） |
| 缩放：普通 ↔ 大 | **右摇杆按压长按 ≥ 0.45 秒** |
| 视角归中 | **长按右手 B** 0.40 秒 |
| 呼唤马 | 按 **左手 X** |
| 系统菜单 | 按 **左手 Menu**（右手 Menu 被 Oculus 系统占用） |

### 区域交互（手柄射线 + 右扳机）

| 位置 | 效果 |
| --- | --- |
| 莲叶 | 弹一个音符；叶片晃动、水面涟漪 |
| 水晶（点按或长按） | 蓄力粒子球；松开触发程序化绽放 |
| 蘑菇区地面 | 点按种 1 棵；长按再松开种 5–8 棵的环形 |
| 已有蘑菇 | 点按培育（+0.35× 缩放，最大 2.4×） |
| 烟花迫击炮 | 发射火带，触发整段烟花表演 |
| 樱花水晶 | 收束水晶球，触发树木生长与花瓣漩涡 |
| 坐骑 | 右手扳机上马（依各动物的尺度门槛），右手 A 下马；左摇杆移动，右摇杆转向 |
| 秋千坐板 | 右扳机坐上去（仅普通尺度，射线需命中坐板）；左摇杆前/后蹬（±60° 摆幅）；右手 A 起身 |

完整对照见 [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md)。

---

## 项目结构

```text
Assets/
  _Project/              # 团队拥有的所有内容
    Art/                 # 着色器、材质、贴图、道具
    Audio/               # 音乐、音效、环境循环
    Characters/          # 生物专属资源
    Core/                # 共享运行时
      Runtime/           #   - GameFlowManager、ParkAttractionState
      XR/                #   - XR 原点、舒适配置、归中、射线代理、触觉、性能引导
    Editor/              # 编辑器生产工具
    Features/            # 模块化玩法系统（一种玩法一个目录）
      CherryGarden/      #   - 运行时水晶球 + 树木生长 + 花瓣漩涡
      Fireworks/         #   - 魔法迫击炮 + 发射台 + 点云表演
      Growth/            #   - 蘑菇种植与培育
      LotusPond/         #   - 七音音律
      Mounts/            #   - 猫/狗/马骑乘控制器、马匹呼唤、导览蝴蝶
      ParticleVitality/  #   - 粉色水晶：花瓣 / 花粉魔法
      ScaleShift/        #   - 普通 / 小 / 大 三种玩家尺度
      Weather/           #   - 天气预设与区域响应
    UI/                  # 世界空间 UI：欢迎面板、系统菜单、告示牌、本地化（中/英/瑞典语）
    World/               # 主场景、地形、各区域、共享世界美术
      Persistent/        #   - World_WonderlandPark.unity（生产场景）
      Regions/           #   - 各区域装配内容
        CatRoute/        #     （场景内根：Region_CatGarden）
        FireworksClearing/  #  （场景内根：Region_FireworksClearing —— 含瀑布 + 烟花）
        FlowerField/     #     （场景内根：Region_FlowerGarden —— 粉色水晶）
        HumanEntry/      #     （装配内容；入口实际由 UI/WelcomePanel 实现）
        LotusPond/       #     （场景内根：Region_LotusPond）
        MushroomGrove/   #     （场景内根：Region_MushroomGrowth）
        Terrain/         #     （地形块内容）
      Shared/            #   - 共享灯光 / 音频 / 材质
Builds/Windows/          # 最近一次的 Windows 构建（WONDERFULWORLD.exe）
Docs/                    # 生产文档（英文）
Packages/                # Unity 包清单
ProjectSettings/         # Unity 项目设置（Linear、SPI、IL2CPP 等）
```

第三方内容（Toon Fantasy Nature、NamuFX、ithappy、XR Interaction Toolkit 示例）保留在各自的供应商目录内，由生产场景**引用**而不是复制。

---

## 性能目标

运行目标是 Quest 3 + Link 线缆。

| 指标 | 最低 | 目标 |
| --- | --- | --- |
| 头显刷新率（稳定） | 72 Hz | 90 Hz |
| 渲染缩放 | 1.0 | 1.0 |
| MSAA | 4× | 4× |
| HDR | 关闭 | 关闭 |
| 不透明纹理 | 关闭（除非必要） | 关闭 |
| SRP Batcher | 开启 | 开启 |
| 立体渲染 | Single Pass Instanced | Single Pass Instanced |

性能剖析与分诊流程见 [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md)。

---

## 文档

所有维护文档统一放在 [`Docs/`](Docs/) 目录，按团队规定仅以英文撰写：

- [Project Overview](Docs/PROJECT_OVERVIEW.md) —— 产品定位、目标平台、当前场景、区域清单
- [Build & Run](Docs/BUILD_AND_RUN.md) —— Unity 版本、Quest 3 Link 流程、冒烟测试步骤
- [System Structure](Docs/SYSTEM_STRUCTURE.md) —— 目录布局、主场景层级、核心预制体、运行时系统
- [Interaction Bindings](Docs/InteractionBindings.md) —— 生产场景内所有面向玩家的可交互体（已对照脚本逐项校对）
- [Cleanup & Standardization](Docs/CLEANUP_AND_STANDARDIZATION.md) —— 层级、资源、命名、文档规范
- [Asset Reference Audit](Docs/Asset_Reference_Audit.md) —— 当前外部依赖快照
- [VR Performance Guide](Docs/VR_PERFORMANCE_GUIDE.md) —— 性能剖析流程、预算与分诊步骤
- [Scale Shift Controller Flow](Docs/ScaleShiftCharacterControllerFlow.md) —— 缩放过程中 `CharacterController` 的安全突变顺序
- [Final Release Checklist](Docs/FINAL_RELEASE_CHECKLIST.md) —— 编辑器、Play Mode、Quest 3 Link 的签收清单

---

## 编辑器工具

生产编辑器工具位于 Unity 菜单 **Wonderful World > Production**：

- *Create Standard Project Folders*
- *Generate Production Audit*
- *Generate Asset Reference Audit*
- *Internalize Referenced Temp Art*
- *Normalize Main Scene Hierarchy*

任何 Unity 资源的移动与重命名必须经过 **Project 窗口** 或 `AssetDatabase`，**绝不允许在操作系统层面拖拽**，以保证 `.meta` 与 GUID 引用不被破坏。

---

## 致谢

Wonderland 大量依赖授权良好的第三方资源，主要包括：

- **Toon Fantasy Nature** —— 风格化环境美术（树木、岩石、亭子、秋千、装饰物）
- **NamuFX – Stylized Water Effects** —— 水材质、涟漪、飞溅与气泡
- **ithappy – Animals FREE** —— 猫、狗、马的网格、材质与动画控制器
- **Unity XR Interaction Toolkit – Starter Assets** 与 **XR Device Simulator** —— 手柄预制体、传送瞄准环、隧道晕影源、手势捕捉
- **Liberation Sans（TextMesh Pro）** —— 兜底字体
- **Butterfly（Ulysses）** —— 蝴蝶基础网格与动画控制器
- **freesound.org – `jaz_the_man_2`** —— 莲池音符样本（`do`、`re`、`mi`、`fa`、`sol`、`la`、`si`）

所有供应商内容保留在 `Assets/` 下各自原始目录中。完整依赖快照见 [`Docs/Asset_Reference_Audit.md`](Docs/Asset_Reference_Audit.md)。

---

## 协作

本仓库是 v1.0.0 版的生产源。提交 PR 前请：

1. 从 `main` 分支切出特性分支。
2. 在 Unity `6000.3.12f1` 中打开并确认无编译错误。
3. 跑一遍 **Wonderful World > Production > Generate Production Audit** 与 **Generate Asset Reference Audit**。
4. 用 Quest 3 Link 跑一遍 [冒烟测试](Docs/BUILD_AND_RUN.md#smoke-test)。
5. 尽量将层级、资源组织、文档与性能改动拆到独立 commit。

---

## 许可

本项目以 [MIT 许可证](LICENSE) 发布。第三方资源遵循各自原授权 —— 详见 `Assets/` 下各供应商目录及 [Asset Reference Audit](Docs/Asset_Reference_Audit.md)。
