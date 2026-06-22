# Artisan Mod 维护说明

更新时间：2026-06-22

## 当前状态

- 当前项目源码基线：`4.0.5.15` + `upstream/main` 后续提交 `62d4b5c`。
- 当前 fork 维护内容：保留主要 UI 与运行提示汉化，并补充 ICE 可通过 IPC 按配方覆盖 Raphael 宇宙稳手最大使用次数。

## 变更记录

### 2026-06-10

- 迁移并补全 `4.0.5.12` 源码基线的主要界面、列表、宏、模拟器、求解器、耐力模式、部队工坊、Teamcraft 导入、Raphael 缓存等用户可见文本。
- 术语优先沿用旧汉化与游戏官方用语，例如耐力模式、作业精度、加工精度、制作力、掌握、集中加工、集中制作、能工巧匠图纸、部队指南等。
- **本地化调整**：
  - `优质/卓越` → `高品质/最高品质`（Good/Excellent 状态名称）
  - `开场` → `起手`（Opener）
  - `阈值1/2/3` → `第1/2/3档`（品质断点）
  - `延后进度` → `后置进度`（BackloadProgress）
  - `普通(Normal状态)` → `通常`
  - `高品质(GoodOmen)` → `好兆头`
  - `坚实(Robust)` → `强韧`
  - `制作日志` → `制作笔记`（统一为游戏内 Crafting Log 官方译名）
  - `CraftingList.cs` 中 6 处英文错误提示已补全中文翻译
  - `AtkResNodeFunctions.cs` 中"Simulated Starting Quality"→"模拟起始品质"
  - `PremadeLists.cs` 中"Lv."→"等级"
  - 涉及文件：`Simulator.cs`、`RaphaelSolver.cs`、`ExpertSolverSettingsUI.cs`、`ExpertProfilesUI.cs`、`SimulatorUI.cs`、`SimulatorUIVeynVersion.cs`、`MacroEditor.cs`、`CraftingList.cs`、`AtkResNodeFunctions.cs`、`PremadeLists.cs`

### 2026-06-11

- 同步 `4.0.5.14` 相关维护补丁，并保留中文化 Mod 改动。
- 补齐半手动模式的“执行接下来 X 个动作”队列功能，并在 UI 设置中加入对应开关与数量配置；新增中文按钮与说明文本。
- 补齐 `UseDoNextX`、`DoNextXAmount` 配置字段。
- 补齐 `RecipeInformation` 中 `38254`-`38261` 不可完成配方 ID，避免制作笔记完成度误判。
- 补齐特殊条件专家配方判定修正：当条件标志为 `15` 且配方标记为专家时，取消专家处理。
- 将 `AutoRetainerIPC` 调整为可释放实例，并在主插件生命周期中初始化/释放。

### 2026-06-12

- 检查并合并公开上游 `upstream/main` 的 `4.0.5.15` 更新，包含 `bb4225c`（新染料配方快速制作修复）和 `06cdb75`（Raphael/模拟器等修复）。
- 将项目版本更新为 `4.0.5.15`，并同步 `Artisan.json` 中的 4.0.5.15 更新日志。
- 接入上游新增的 `AutoRetainerAPI` 子模块与项目引用，同时保留本地 `AutoRetainerIPC` 生命周期处理。
- 合并 Raphael 失败提示逻辑：未解锁“掌握”时显示中文专门提示，其它失败继续输出可报告的 Raphael 参数。
- 合并图形模拟器 Raphael 生成状态修复：生成中的任务使用 `RaphaelCache.Tasks` 检查，避免重复启动生成；相关按钮与提示保留中文。
- 合并半手动“执行接下来 X 个动作”队列逻辑时保留中文按钮，并去除 `Configuration` 中上游与本地重复引入的 `UseDoNextX` / `DoNextXAmount` 字段。

### 2026-06-13

- 增加 Raphael 专用的按配方宇宙稳手最大使用次数覆盖，不复用专家求解器的 `ChangeExpertMaxSteadyUses` 字段。
- `RecipeConfig` 新增 `TempRaphaelMaxStellarHand`（临时、不序列化）和 `raphaelMaxStellarHand`（永久、可保存）字段；优先级为临时配方覆盖、永久配方覆盖、全局 `RaphaelSolverConfig.MaxStellarHand`。
- 新增 IPC：
  - `Artisan.ChangeRaphaelMaxStellarHand(uint recipeId, uint maxUses, bool temporary)`
  - `Artisan.SetTempRaphaelMaxStellarHandBackToNormal(uint recipeId)`
- `RaphaelSolver` 新增统一 helper 计算最终稳手次数，生成参数 `--stellar-steady-hand`、`RaphaelOptions.SteadyHandUses`、缓存 key 与宏名称均使用同一结果，避免参数和缓存不一致。
- Raphael 下拉区域增加只读提示，显示当前 Raphael 宇宙稳手上限与本次可用次数。
- 未添加奇迹之材支持；奇迹之材仍只适合专家求解器按当前状态逐步计算，不适合 Raphael 预生成宏。

### 2026-06-16

- 新增 `标准配方求解器（奇迹专家）`，保留原 `标准配方求解器` 行为不变。
- 新求解器平时沿用标准配方求解逻辑；进入奇迹之材增益期间后，直接使用已套用专家配置的专家配方求解器结果，避免再被标准求解器的低耐久兜底逻辑改写。
- 新求解器在低耐久且掌握已生效时保留可触发掌握回复的 0 耐久行动，避免将观察等行动简单改写为精修。
- 涉及文件：`CraftingProcessor.cs`、`StandardSolver.cs`。

### 2026-06-18

- 新增 `专家配方求解器（保守神技）`，保留原 `专家配方求解器` 行为不变。
- 新求解器复用原专家求解器主体逻辑，但在 10 层内静品质阶段限制 `工匠的神技` 连续消耗 CP；已使用一次后，后续神技推荐会优先改为高品质时使用秘诀，其他情况使用观察等待更好条件。
- `Artisan.ChangeSolver` 增加英文别名 `Expert Recipe Solver (Conservative Finesse)`，方便外部 IPC 调用新求解器。
- 涉及文件：`CraftingProcessor.cs`、`ExpertSolver.cs`、`IPC.cs`。

### 2026-06-22

- 合并 `upstream/main` 的后续更新：`4ca1ae2`（Raphael 缓存改为按角色保存）和 `62d4b5c`（模拟器运行专家求解器时套用专家配置文件）。
- 接入上游 `RaphaelCache.CurrentCache` 结构，Raphael 宏缓存从主配置字段迁移为按当前角色独立读写；缓存 UI 和缓存表继续保留中文显示。
- 保留本地 Raphael 宇宙稳手覆盖逻辑：`RaphaelOptions.SteadyHandUses` 仍统一使用配方临时覆盖、配方永久覆盖、全局设置后的最终值，避免不同稳手上限共用错误缓存。
- 合并模拟器专家配置文件支持，GUI 模拟器与 Veyn 模拟器运行专家类求解器时会套用对应配方的专家 Profile。
- 保留本地新增求解器 `标准配方求解器（奇迹专家）` 与 `专家配方求解器（保守神技）`。
