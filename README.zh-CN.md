# Wonderland 仙境

一座手作的 PC VR 奇幻乐园 —— 第一人称、舒适优先的奇境，住着蝴蝶、莲池乐音、花朵魔法、温顺坐骑、生长的蘑菇与盛放的樱花树。

**语言：** [English](README.md) · **中文** · [Svenska](README.sv.md)

---

## 项目简介

Wonderland（项目内部也称 *Butterfly House* / *Wonderful World*）是一款基于 **Unity 6** + **OpenXR** + **通用渲染管线（URP）** 构建的单人 VR 探索体验。玩家在一个看似连贯无缝的乐园切片中漫游，七个相互连通的主题区构成一个个独立的小奇迹，而非一条任务链。

我们的设计准则始终如一：

1. **舒适优先**。稳定的帧节奏、隧道式视野晕影、默认传送、缩放过程的眨眼黑屏过渡、不强加被动位移。
2. **可探索的惊喜**。每个区域都是值得被发现的独立交互，而不是必须打卡的关卡。
3. **风格化，不写实**。基于 Single Pass Instanced URP 的卡通着色（Toon Fantasy Nature + 自研着色器），专门面向 Quest 3 Link 调校。

当前的生产场景是 [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity)，发布版本为 **v1.0.0**。

---

## 亮点

- **七个主题区域、一个连贯的乐园** —— 人界入口、花园、莲池、猫园、烟花空地、蘑菇生长地、樱花庭。
- **三种身体尺度** —— 在 普通 / 小尺度（0.25×）/ 大尺度（1.75×）之间切换，0.4 秒眨眼过渡；视高、移动速度、交互距离会自动适配。
- **莲池音律** —— 七片浮叶按 *do · re · mi · fa · sol · la · si* 自然大调音阶调音，从手柄发出弧形的水之魔法击中莲叶来弹奏；额外的"曲目触发器"会随机抽一首曲子让玩家跟弹。
- **猫园坐骑系统** —— 三只独立坐骑（猫、狗、马），各自的骑乘路径、待机踱步、悬停描边与靠近时的叫声。骑乘需要切换到 **小尺度**。马匹可通过 **左手 X** 在任意位置呼唤前来；猫会在靠近时自动上骑。
- **导览蝴蝶** —— 三只实时蝴蝶，玩家骑乘靠近时会沿样条飞行点起飞。
- **花瓣 & 花粉魔法** —— 右扳机长按对准大花朵，粒子会沿二次贝塞尔曲线汇聚到你眼前的悬浮球；释放后会进入六种程序化爆发之一：`SpiralBloom`、`MathRibbon`、`TornadoVortex`、`AizawaFountain`、`DreamAttractor`、`GalaxyVeil`。
- **蘑菇种植** —— 点按种下一棵；长按蓄力释放则在落点周围环形生成 5–8 棵。对已有蘑菇再次扣扳机即可"培育"使其长大。
- **烟花终章** —— 瞄准魔法迫击炮，发射螺旋火带沿三次贝塞尔曲线飞向装置，触发整段点云烟花表演。
- **樱花水晶球** —— 樱花树上方运行时生成的水晶球，激活后播放四段树木生长动画与花瓣漩涡。
- **Quest 3 Link 舒适层** —— 专门的舒适移动配置文件、长按归中、骑乘感知的视角归中、按模式切换的隧道晕影。

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

### 准备环境

- Windows 10/11，具备 VR 性能的 GPU
- Meta Quest 3 + Link 线缆（或受支持的 USB-C 线缆）
- [Meta Quest Link](https://www.meta.com/quest/setup/) 桌面端
- Unity `6000.3.12f1`（通过 Unity Hub 安装）
- Git，建议同时安装 [Git LFS](https://git-lfs.com/) 用于美术资源

### 克隆仓库

```bash
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
```

### 在 Unity 中打开

1. 打开 **Unity Hub** → *从磁盘添加项目* → 选择本目录。
2. 使用 Unity `6000.3.12f1` 打开，等待首次资源导入完成（`Library/` 会在本地重建）。
3. 确认 Console 没有编译错误。
4. 打开生产场景 **[`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity)**。

### 在 Quest 3（Link 模式）上运行

1. 用 Link 线缆连接 Quest 3，确认 *Meta Quest Link* 桌面端识别到头显。
2. 在头显内进入 **Quest Link** 串流模式。
3. 在 Unity 中点击 **Play**，XR 原点应已开始追踪你的头部与双手。
4. 也可直接运行已构建好的 Windows 可执行文件 [`Builds/Windows/WONDERFULWORLD.exe`](Builds/Windows/WONDERFULWORLD.exe)。

> 收尾版构建目标：**Windows / x86_64 / IL2CPP / Linear 色彩空间 / Single Pass Instanced**。

---

## 操作速查

### 全局

| 行为 | 输入 |
| --- | --- |
| 传送（默认） | 向前推 **左摇杆** → 松开 |
| 平滑移动（备选） | 推 **左摇杆** |
| 转向：定角（默认） | **右摇杆** 左/右（每次 30°） |
| 转向：平滑（备选） | **右摇杆** 左/右 |
| 缩放：普通 ↔ 小 | **右摇杆按压双击** |
| 缩放：普通 ↔ 大 | **右摇杆按压长按 0.45 秒** |
| 视角归中 | **长按右手 B** 0.40 秒 |
| 呼唤马 | 按 **左手 X** |
| 暂停 / 系统菜单 | 预留给 **左手 Menu** 键（右手 Menu 被 Oculus 系统占用） |

### 区域交互（手柄射线 + 右扳机）

| 位置 | 效果 |
| --- | --- |
| 莲叶 | 弹一个音符；叶片晃动、水面涟漪 |
| 大花朵（长按） | 蓄力粒子球；松开触发程序化绽放 |
| 蘑菇区地面 | 点按种 1 棵；长按再松开种 5–8 棵的环形 |
| 已有蘑菇 | 点按培育（+0.35× 缩放，最大 2.4×） |
| 烟花迫击炮 | 发射火带，触发整段烟花表演 |
| 樱花水晶球 | 收束水晶球，触发树木生长与花瓣漩涡 |
| 坐骑（仅小尺度） | 右手 A 下马；左摇杆移动，右摇杆转向 |

完整对照见 [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md)。

---

## 项目结构

```text
Assets/
  _Project/              # 团队拥有的所有内容
    Art/                 # 着色器、材质、贴图、道具
    Audio/               # 音乐、音效、环境循环
    Characters/          # 生物专属资源
    Core/                # 共享运行时（XR 原点、舒适配置、归中等）
    Editor/              # 编辑器生产工具
    Features/            # 模块化玩法系统（一种玩法一个目录）
      CherryGarden/      #   - 运行时水晶球 + 树木生长 + 花瓣漩涡
      Fireworks/         #   - 魔法迫击炮 + 烟花表演
      Growth/            #   - 蘑菇种植与培育
      LotusPond/         #   - 七音音律
      Mounts/            #   - 猫/狗/马骑乘控制器 + 导览蝴蝶
      ParticleVitality/  #   - 花瓣 / 花粉魔法
      ScaleShift/        #   - 普通 / 小 / 大 三种玩家尺度
      Weather/           #   - 天气预设与区域响应
    UI/                  # 世界空间 UI、告示牌、本地化、系统菜单
    World/               # 主场景、地形、各区域、共享世界美术
      Persistent/        #   - World_WonderlandPark.unity（生产场景）
      Regions/           #   - 各区域装配内容（FlowerField、LotusPond……）
      Shared/            #   - 共享灯光 / 音频 / 材质
Builds/Windows/          # 最近一次的 Windows 构建
Docs/                    # 生产文档（英文）
Packages/                # Unity 包清单
ProjectSettings/         # Unity 项目设置（Linear、SPI、IL2CPP 等）
```

第三方内容（Toon Fantasy Nature、NamuFX、ithappy、XR Interaction Toolkit 示例）保留在各自的供应商目录内，由生产场景**引用**而不是复制。

---

## 性能目标

运行目标是 Quest 3 + Link 线缆。**帧节奏比平均 FPS 更重要** —— 任何掉帧、撕裂、黑屏闪烁、抖动都视为发布阻断项。

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

- [Project Overview](Docs/PROJECT_OVERVIEW.md) —— 产品定位、目标平台、当前场景、功能清单
- [Build & Run](Docs/BUILD_AND_RUN.md) —— Unity 版本、Quest 3 Link 流程、冒烟测试步骤
- [System Structure](Docs/SYSTEM_STRUCTURE.md) —— 目录布局、主场景层级、核心预制体、运行时系统
- [Interaction Bindings](Docs/InteractionBindings.md) —— 生产场景内所有面向玩家的可交互体
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

本仓库是 v1.0.0 收尾版的生产源。提交 PR 前请：

1. 从 `main` 分支切出特性分支。
2. 在 Unity `6000.3.12f1` 中打开并确认无编译错误。
3. 跑一遍 **Wonderful World > Production > Generate Production Audit** 与 **Generate Asset Reference Audit**。
4. 用 Quest 3 Link 跑一遍 [冒烟测试](Docs/BUILD_AND_RUN.md#smoke-test)。
5. 尽量将层级、资源组织、文档与性能改动拆到独立 commit。

---

## 许可

项目源码与团队原创资源版权归 Wonderland 团队所有。第三方资源遵循各自原授权 —— 详见 `Assets/` 下各供应商目录及 [Asset Reference Audit](Docs/Asset_Reference_Audit.md)。

---

*用心面向头显而作。*
