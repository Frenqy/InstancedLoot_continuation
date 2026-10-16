# 开发、构建与验证

## 开发环境

项目是 SDK 风格 C# 类库，目标 `netstandard2.1`，`LangVersion=default`。源码使用 C# 12 集合表达式，例如 `[..players]`，因此编译器需要支持 C# 12；可从 .NET 8 或更新 SDK 开始。仓库没有 `global.json` 锁定 SDK。目标框架是游戏中加载程序集的兼容目标，不代表必须安装同版本的现代 .NET 运行时才能玩模组。

本次实际使用 Windows PowerShell 和 **.NET SDK 10.0.201** 完成 Release 构建。编译无需打开 Unity Editor；运行验证需要安装游戏、BepInEx 及运行依赖。

## 编译依赖与运行依赖

项目文件中显式声明的 NuGet 包：

| 包 | 版本 |
| --- | --- |
| BepInEx.Core | `5.4.21`，固定 BepInEx 5 编译 API |
| RiskOfRain2.GameLibs | `1.5.0-r.1` |
| UnityEngine.Modules | `2021.3.33` |
| MMHOOK.RoR2 | `2026.10.8`，该引用抑制 NU1701 |
| R2API.Core | `5.3.0` |
| R2API.Networking | `1.0.4` |

`nuget.config` 添加 `https://nuget.bepinex.dev/v3/index.json` 与 `https://api.nuget.org/v3/index.json`；不是包锁文件，也没有清空上层可能存在的其他源。

`manifest.json` 的运行依赖是 `bbepis-BepInExPack-5.4.2122`、`RiskofThunder-HookGenPatcher-1.2.9`、`RiskofThunder-RoR2BepInExPack-1.46.0`、`RiskofThunder-R2API_Core-5.3.0`、`RiskofThunder-R2API_Networking-1.0.4`。NuGet 的 BepInEx.Core 是编译 API，Thunderstore 的 BepInExPack 是运行时整合包，版本号不能互换。显式声明 RoR2BepInExPack 1.46.0，是因为新版 R2API.Core 需要它，而 BepInExPack 自身只要求历史最低版本。

### Hallowed Concepts 适配目标与来源

2026-10-15 在线核对后，本次目标为 **2026-10-08 发布的 Hallowed Concepts / 游戏 1.5.0**；“万圣节更新”按这个实际发布名称处理，不由当前日期猜测。依据是 [2K 官方补丁说明](https://support.2k.com/hc/en-us/articles/56193476123795-Risk-of-Rain-2-Patch-Notes-October-8-2026) 的发布日期/名称，以及 [Risk of Thunder 的 RoR2EditorKit 5.7.0 发布记录](https://github.com/risk-of-thunder/RoR2EditorKit/releases/tag/5.7.0) 对 Hallowed Concepts 与 1.5.0 的明确对应。本次没有实现 DLC4 六种新交互物的专用支持。

依赖选型依据实时包元数据，而非搜索引擎缓存的“最新版本”：

- [BepInEx GameLibs 注册索引](https://nuget.bepinex.dev/v3/registration/riskofrain2.gamelibs/index.json) 列出 `1.5.0-r.1`，发布时间 2026-10-09；[MMHOOK 实时版本索引](https://api.nuget.org/v3-flatcontainer/mmhook.ror2/index.json) 列出 `2026.10.8`。两者用于同一 1.5.0 API 编译目标。
- [R2API.Core 5.3.0](https://www.nuget.org/packages/R2API.Core/5.3.0) 与 [R2API.Networking 实时版本索引](https://api.nuget.org/v3-flatcontainer/r2api.networking/index.json) 的 1.0.4 是核对时的最新版。它们最初针对 1.4.x 发布，不能仅凭“最新版”宣称运行于 1.5.0 已验证；显式 GameLibs/MMHOOK 引用覆盖其较旧的传递编译依赖。
- 运行包分别核对 [BepInExPack](https://thunderstore.io/api/experimental/package/bbepis/BepInExPack/)、[HookGenPatcher](https://thunderstore.io/api/experimental/package/RiskofThunder/HookGenPatcher/)、[RoR2BepInExPack](https://thunderstore.io/api/experimental/package/RiskofThunder/RoR2BepInExPack/)、[R2API.Core](https://thunderstore.io/api/experimental/package/RiskofThunder/R2API_Core/) 和 [Networking](https://thunderstore.io/api/experimental/package/RiskofThunder/R2API_Networking/) 的 Thunderstore API。RoR2BepInExPack 1.46.0 发布于 2026-10-12。

1.5.0 API 适配包括轮盘箱 `EjectPickupServer(UniquePickup)`、选择器 `CreatePickup(UniquePickup)` 与 `onUniquePickupSelected`，避免将携带临时/租用/升级状态的拾取物降为单一 PickupIndex。选择器同时兼顾旧 prefab 的 `onPickupSelected` 销毁事件；共享选择时通过 SetPlayers 同步剩余资格，迁移 Hook 的来源上下文用 try/finally 恢复。普通拾取物检查从 `pickup.pickupIndex` 读取物品类型。

对 droplet 的既有 IL Hook 检查确认其目标已经是 `CreatePickupDroplet(CreatePickupInfo, Vector3, Vector3)`，并未 Hook 废弃的 PickupIndex 重载；普通物品 `CreatePickup(in CreatePickupInfo)` 也仍为当前签名。旧真实 DLL 证明各 droplet 重载汇合到该入口、落地和强制初始化均进入 `CreatePickup()`，因此来源上下文改包围 CreatePickup 而非仅 OnCollisionEnter，涵盖强制初始化。旧 DLL 的 AttemptGrant 两次读取 GrantContext.shouldDestroy（设置 consumed、最终销毁）；原补丁只改第一次读取的返回值。本次改为在首次读取前按引用更新整个 GrantContext，让成功领取且尚有资格者时两处判断一致保留对象；授予失败时不移除资格。新增成对注册/卸载的 On.AttemptGrant 在授予前检查资格，阻止直接调用绕过交互/UI 检查；资格与领取结果逻辑集中在不安装 Hook 的 PickupGrantPolicy 辅助类，供隔离回归直接链接。更新后 build `25921972` 的真实方法体再次确认这些调用链和两次 shouldDestroy 读取；静态匹配与 emitter 结构检查通过，实际 Hook 安装和游戏内执行仍待验证。

## 构建

从仓库根目录执行：

```powershell
dotnet --version
dotnet restore RoR2Mods.sln
dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --no-restore
```

也可以用一条带隐式还原的命令：

```powershell
dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release
```

输出 DLL：`InstancedLoot/bin/Release/netstandard2.1/InstancedLoot.dll`。安装时复制到游戏的 `BepInEx/plugins/InstancedLoot/`；联机各端应使用同一构建产物。

默认采用 Release。Debug 会注册 `DevelopmentHooksHandler`：替换 Steam 客户端连接回调为空实现、把 SyncInstances 网络 hash 固定为 1337、补玩家显示名等。Debug 不是仅增加调试符号，不能拿它直接代表发布版联机行为。

## 本次验证记录

以下为历史文档整理记录，代码基线 `46427bb`，日期 2026-10-15：

执行 `dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --nologo`，包含还原，**构建成功，0 错误、4 警告**：

| 警告 | 位置 | 内容 |
| --- | --- | --- |
| CS0618 | `Hooks/SceneDirectorHandler.cs` | Inventory.GetItemCount(ItemDef) 已过时 |
| CS0618 | `Hooks/PickupPickerControllerHandler.cs` | onPickupSelected 已过时 |
| CS0618 | `InstancedLoot.cs` | GenericPickupController.pickupIndex 已过时 |
| CS0414 | `Components/FadeBehavior.cs` | lastCameraRigController 赋值后未使用 |

未运行游戏、联机/分屏测试或发布打包脚本。此记录仅证明该环境下的编译和依赖还原可完成，未证明运行时 IL 匹配适配当前已安装游戏。

初次 1.5.0 适配记录（基于 `1127e98`，2026-10-15）：`dotnet restore RoR2Mods.sln --nologo` 成功；最终 Release `--no-restore` 构建 **0 错误、1 警告**（SceneDirectorHandler 的旧 Inventory.GetItemCount API）。首次构建暴露的轮盘箱 UniquePickup 签名 4 个错误已修复。隔离 MultiplayerRegression **33 个断言通过**；PickupGrantILRegression 默认合成方法 **15 个结构断言通过**，另对本机旧真实 DLL 检查 **7 个结构断言通过**，合计 22。`git diff --check` 通过。未执行打包、上传或发布，也未执行游戏内测试。

GameLibs 1.5.0-r.1 内的方法体为 `throw null` 的编译桩，反编译仅用于核对字段、事件和方法签名，不能证明 IL 游标或运行行为兼容。初次适配时本机 Steam 安装为 build `21587608`，当时记录的待更新目标为 `25475991`；该阶段的静态分析属于旧安装证据，不能当作 1.5.0 运行验证。

### 更新本机游戏后的静态验证（2026-10-15）

用户更新后，本机 `appmanifest_632360.acf` 的 buildid 与 TargetBuildID 均为 **25921972**；`globalgamemanagers` 的游戏版本为 **1.5.0**。检查对象是安装目录 `D:/SteamLibrary/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/Managed/RoR2.dll`，SHA256 为 `48F2758CCCBE1A5EFBA88CFFE0A68631953F489A71A92726572C15A4C57FF90C`。这个 build 是当前本机证据，不覆盖其他安装或后续热修复。

真实方法体检查带来两项修复：`InteractableSpawnCard.Spawn` 现在读取请求的 placementRule，复制请求已显式提供 `DirectWithoutRandomRotation`，防止旧 null 规则导致异常并保留源旋转；`AnimationEvents.CreatePrefab` 已把 Instantiate 移入编译器生成的 DoSpawnEffect 局部函数，旧外层匹配为零。动画 Hook 现在捕获 ShouldUsePooledEffect 的 bool 局部，在 **4 个**返回 Transform 的调用后注入，保留原返回值，并只把非池化效果关联到 FadeBehavior。池化对象可能复用给其他拥有者，当前没有实现其租借/归还生命周期的显示关联。

最终执行结果：

| 检查 | 结果与边界 |
| --- | --- |
| Release `--no-restore` 构建 | **0 错误、1 警告**，仍为 SceneDirectorHandler 的旧 Inventory.GetItemCount API |
| MultiplayerRegression | **37 个断言通过**，增加了非空 placementRule、放置模式和源 transform 保留检查；仍是 stub 环境 |
| PickupGrantILRegression | 合成 pickup **15** + 当前真实 AttemptGrant **7** 个结构断言通过；直接链接生产 emitter |
| AnimationPrefabIL 结构检查 | 生产 emitter 在合成方法与当前真实 CreatePrefab 各 **4 个返回点**保留 Transform，按顺序传 self 和原 bool 局部；另拒绝 **4 类**缺失/变更模式 |
| 已注册 Release IL 模式审计 | 从当前 HookManager 与已注册 ObjectHandler 的源码登记关系提取 **17 项**，全部找到预期模式；动画项调用生产 emitter，其余模式审计不执行完整注入 |
| `git diff --check` | 通过 |

审计仅在内存读取/转换真实 DLL，没有安装模组、修改游戏 DLL、配置或存档，没有启动游戏，也没有打包/发布。它没有让 Unity/Mono 安装全部 Hook 或执行注入后方法；双端联机、分屏和下表场景仍未实测。

额外发现 `PickupPickerController.AttemptGrantPickupDirectly(UniquePickup)` 可通过 ItemTransferOrb 或 PickupDef 直接授予，正常分支不经过现有 CreatePickup/GenericPickupController.AttemptGrant Hook；只有缺少参与者、body 或 PickupDef 等回退情况才进入 CreatePickup。尚未核对哪些 prefab/UnityEvent 资产使用它，因此记录为未覆盖路径，不能把它当作已确认触发的共享选择器缺陷，也不能宣称所有新选择器已支持。

### 实际启动失败后的指挥精华 IL 修复（2026-10-15，归入模组 1.5.1）

上面的 17 项模式审计是修复前的历史结果，未验证全部注入后的控制流。用户随后实际运行在 `IL.RoR2.PickupDropletController.add_CreateCommandCube` 安装阶段收到 `InvalidProgramException: Invalid IL code ... IL_00a1: call`；这是真实失败，不能再把模式匹配通过当作启动可用证据。

再次核对本机游戏仍为 build `25921972`，`RoR2.dll` SHA256 与上文相同；此时 appmanifest 的 TargetBuildID 为 `0`。原方法 `IL_0085: brfalse.s IL_009d` 在 maxOptionCount 为空时直接跳到单参数 `NetworkServer.Spawn(GameObject)`。旧注入把 dup 放在 Spawn 前，但分支仍指向原 Spawn；该路径跳过 dup，Spawn 消费唯一 GameObject 后回调下溢。Spawn 重载参数没有发生本次怀疑的变化。注入主体已存在于 `1127e98`，blame 指向 `c547cc7a`（2024-06-08）；此次 1.5.0 适配没有引入该 Hook，但此前检查漏检了它。

生产注入提取为 `CommandCubeIL`，精确检查静态单参数 void Spawn，再用 `MoveAfterLabels` 将原 Spawn 的 incoming labels 转到 dup，保留原生初始化及 Spawn 后的来源回调。没有关闭 Hook 或禁用指挥精华。新增回归通过 `ILContext.Invoke` 执行实际生产 emitter，包含 MonoMod 的标签转换流程；此前直接找调用名称的审计项已替换为以下验证：

| 检查 | 结果与边界 |
| --- | --- |
| 当前真实 CreateCommandCube | 原生方法与修正后完整方法的有限指令集栈/类型数据流验证通过；修正后 48 条指令、1 条条件分支均可达，无异常区域；所有原分支目标/指令顺序保留，指向 Spawn 的目标改为 dup |
| 旧注入负例 | 真实方法重放旧注入后检测到 callback stack underflow；两个合成分支方法同时被栈检查与实际 CLR 执行以 InvalidProgramException 拒绝 |
| 新注入执行 | 用 MonoMod Cecil generator 生成内存 CLR 方法，执行 6 条条件、switch、无条件跳转和顺序路径；使用真实 EmitDelegate 捕获闭包路径，确认 Spawn 后回调恰好一次且收到原 GameObject |
| 变更诊断 | 拒绝缺失、额外参数、错误参数类型、非 void 返回、实例方法和多个 Spawn 这 6 类模式变化 |
| 相邻注入点 | droplet 与普通 pickup 的 Instantiate 都具体返回 GameObject，注入后原 consumer 没有 incoming branch；普通 pickup 参数仍为 CreatePickupInfo&。PickupGrant 首次 shouldDestroy 加载及动画四个返回 consumer 均无 incoming branch，无异常区域；未发现此次分支跳过 dup 的同类证据 |
| Release / stub / 既有 IL 回归 | Release 构建 0 错误、1 条既有过时 API 警告；37 条 stub 断言、22 条 pickup 结构断言、动画结构检查及 17 项登记审计通过；普通登记项仍主要验证模式 |

栈验证器只覆盖该完整方法使用的指令/类型/分支，遇到未知指令、不可达区或异常区域即失败，并非通用 CLR verifier。生成方法在 .NET CLR 执行，使用替身 GameObject/NetworkServer；MonoMod 老版 DynamicMethod backend 不兼容现代 .NET 的内部结构，测试进程临时选用其 Cecil backend 并恢复环境值。它没有在 Unity/Mono 中安装修正后的 Hook，也没有执行真实游戏对象、网络或双端场景。下一次游戏验证首先检查插件能否完整启用，再测指挥神器 maxOptionCount 有/无值、两人资格与 SharePickupPickers 两种配置。游戏安装、存档和用户 ZIP 均未改动。

### 模组 1.5.1 版本与规则同步（2026-10-15）

此批仅同步版本元数据、CHANGELOG 和文档规则，没有改动游戏逻辑。执行 `dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --no-restore --nologo`，结果 **0 错误、1 条既有 CS0618 警告**（SceneDirectorHandler 的旧 Inventory.GetItemCount API）。插件属性、项目 Version 与 manifest 均为 1.5.1，生成程序集版本为 1.5.1.0；目标 GameLibs 仍为 1.5.0-r.1。核对 CHANGELOG 顺序、26 个本地文档链接及版本章节锚点，`git diff --check` 通过。未重跑上一节逻辑回归，未启动游戏、打包、上传或发布；上述指挥精华运行验证边界仍成立。

## 打包与版本

```powershell
./build-release.ps1
# 参数可选；正常发布保留 Release
./build-release.ps1 -Configuration Release
```

脚本把工作目录设为自身目录，删除并重建根目录 `Release/`，复制元数据，执行构建并复制 DLL，再根据 manifest 版本压缩。它会覆盖同版本 ZIP。当前产物结构为：

```text
Nicebowl-InstancedLoot-1.6.0.zip
├── CHANGELOG.md
├── icon.png
├── manifest.json
├── README.md
└── InstancedLoot/
    └── InstancedLoot.dll
```

ZIP 前缀 `Nicebowl-InstancedLoot` 是脚本硬编码，manifest 名称是 `InstancedLoot_continuation`，不能从其中一个推断另一个。脚本不复制 `AGENTS.md`、`docs/` 或 `LICENSE`，不会上传发布；README 中的维护文档入口因此使用仓库网页链接。发布时需核对许可证文件的分发要求，当前脚本未包含 LICENSE 是已记录缺口。

| 位置 | 当前版本/用途 |
| --- | --- |
| `InstancedLoot.cs` 的 BepInPlugin 属性 | `1.6.0`，插件元数据 |
| `manifest.json` | `1.6.0`，发布包版本及 ZIP 命名依据 |
| `InstancedLoot.csproj` | `1.6.0`，项目/程序集构建版本元数据 |
| `CHANGELOG.md` | `1.6.0 (Unreleased)` 记录统一权限/购买事务与状态同步重构；`1.5.3 (Unreleased)` 保留三选一终端修复，历史版本继续保留 |

本次将三个模组版本从 1.5.3 统一递增为 1.6.0，保留原 GUID；这是新增统一权限/购买事务边界的 minor 版本。目标游戏仍为 1.5.0，GameLibs 仍为 1.5.0-r.1。上面的 ZIP 名称是按当前版本打包时的预期产物，此次没有执行打包，也没有重建用户已有的 ZIP。CHANGELOG 的 1.5.0 至 1.6.0 都尚未发布，不补写发布日期；历史兼容性声明不能替代具体对象注册检查和游戏实测。

后续每个修改批次/交付必须递增模组版本，包含仅文档或协作规则修改；默认递增 patch，新增功能递增 minor，破坏兼容性递增 major。同一任务的配套修改只递增一次，版本与文档同步是该批次的一部分，不另触发升级，也不要求每次临时编辑或同一批次内的提交分别升级。交付前同步上述三处版本、CHANGELOG、此版本表及其他当前版本描述；保留历史验证记录的原版本与事实。目标游戏与编译/运行依赖版本独立维护，不机械替换。完整约束见 [AGENTS.md](../AGENTS.md)。

### 模组 1.5.2 版本与客户端交互筛选修复（2026-10-15）

本批启用 `InteractorHandler`，并让它从交互者所在 `CharacterBody.master` 解析玩家控制器；重叠箱子候选会在客户端筛掉其他玩家无资格的副本。由于当前候选局部是 `IInteractable`，注入在调用 `Component.GetComponent<InstanceHandler>` 前显式执行 `Castclass Component`。Release 构建成功（0 错误、1 条既有过时 API 警告），MultiplayerRegression 通过 39 条断言；当前游戏 1.5.0 build 25921972 的 `FindBestInteractableObject` 接收者类型检查通过，注册 IL 审计通过 18 个模式和 22 个结构断言。未执行 Unity/Mono Hook 安装、主机/客户端联机或分屏实测，也未打包或改写现有 ZIP。

### 模组 1.5.3 三选一终端与金币购买资格修复（2026-10-16）

三选一副本现在为每个终端建立独立的 `SharedInfo` 组，并将源终端资格限制为源控制器的首位玩家；异步创建的目标终端会复制商品、隐藏、RNG 和最终购买价格后再同步资格。服务器 `PurchaseInteraction.OnInteractionBegin` 在进入原生扣款前检查 `available` 与实例资格，阻止不可用对象、错误玩家和同帧重复调用；商店掉落来源上下文使用 `try/finally` 恢复，避免异常或嵌套调用污染后续掉落。

Release 构建成功（0 错误、1 条既有过时 API 警告）。MultiplayerRegression 通过 48 个断言，覆盖 CopyObject 源/副本资格和每个三选一终端的独立 `SharedInfo` 分组；三选一终端完整状态/权限仍需要在安装游戏的主机、客户端联机矩阵中复测，本批未打包且未改写已有 ZIP。

### 模组 1.6.0 实例权限与购买事务重构（2026-10-16）

权限入口集中在 `InstanceAccessPolicy`：它从 `Interactor`/`CharacterBody` 解析玩家，并调用 `InstanceHandler.IsInstancedFor` 保持 `CopyObject` 按 `OrigPlayer`、`InstancedObject` 按当前 `Players` 集合判定。普通箱、三选一终端、拾取物和客户端候选筛选共用这一层，避免在各 Hook 中重新解释聚合集合。

源/副本购买成本、成本类型和 RNG 快照由 `InstanceStateSynchronizer` 统一复制；三选一控制器仍共享控制器组，而每个终端槽位保持独立 `SharedInfo`。服务器购买 Hook 增加同一对象的重入保护，并在原生逻辑把 `available` 从 true 变为 false 后才消费 `InstancedObject` 的玩家资格；`CopyObject` 每个副本仍由原生单次可用状态消费。隔离回归通过 51 个断言，覆盖普通箱价格/资格、三选一终端分组与终端购买状态快照及共享购买消费。

这批验证未运行真实 RoR2 PayCost、UNet/R2API 传输或主机/客户端联机；原生购买失败时的资格恢复、不同版本游戏的可用状态时序以及三选一实际扣款仍需安装游戏后复测。未打包，也未改写工作树中已有或用户删除的 ZIP。

## 新增交互物/掉落来源

1. 找到游戏真实的生成、初始化与掉落入口，确认名称、组件、SpawnCard 以及服务器执行边界。
2. 在 `Enums.cs` 添加稳定的 ObjectType 字符串、描述、alias、禁用模式；外部模组也可通过配置事件提供元数据。
3. 复用或新增 AbstractObjectHandler，声明类型列表、ObjectInstanceMode、有效对象判断和固有拥有者能力。
4. 需要复制时，在 `InstanceSingleObjectFrom` 保留 RNG、物品、价格、子对象状态，并继续基类的归属/共享状态处理；只有掉落来源时可用 ObjectInstanceMode.None。
5. 在 ObjectHandlerManager 中登记处理器；专用 Hook 可在其 `Init` 中向 HookManager 注册。
6. Hook 初始化路径应先接管 AwaitedObject，再处理正常对象；识别到来源后进入 HandleInstancing。补齐掉落上下文，不能只给箱子挂 Handler 而遗漏最终 pickup。
7. 若引入临时上下文，保存先前值并在 finally 恢复；注册/卸载成对处理。
8. 检查配置能力、预设、网络、显示及多人验证，更新代码导航、配置与问题文档。

外部集成入口是静态 `InstancedLoot.PreConfigInit`，会传入插件实例。可扩展 `ModConfig.GenerateObjectTypes`、`GenerateAliases`、`LimitInstanceModes`、`GenerateObjectInstanceModes`、`GenerateConfigPresets` 及描述事件。注意该事件发生在 Start，而核心 Hook 在 OnEnable 已安装：此时新增 HookHandler 不会自动补装，集成必须明确安排注册时机和卸载。

## 游戏版本升级定位

编译 API 检查只覆盖一部分兼容性。大量 Hook 使用 `ILCursor.GotoNext` 匹配字段、调用和局部变量；游戏逻辑变化后可能编译通过但启动安装 Hook 失败。先根据 BepInEx 日志中的堆栈找到具体 Handler，再核对对应游戏程序集的方法签名及 IL，最后更新依赖和匹配逻辑。不要仅因 changelog 写了 DLC 支持就补写未实测的兼容结论。

## 按改动选择验证

仓库有 `validation/MultiplayerRegression` 隔离 stub 回归程序，直接链接相关生产源码，使用 .NET 8 或更新 SDK 执行：

```powershell
dotnet run --project validation/MultiplayerRegression --configuration Release
```

它验证确定性的同步、归属、价格和相机状态逻辑；stub 不模拟真实 UNet/R2API 传输、RoR2 IL、Unity 渲染或两个游戏客户端。没有 Unity 或自动化联机测试、CI 配置。文档变更检查路径、符号和 diff；代码变更先 Release 构建和相关隔离回归，再按影响范围选择以下游戏场景。结果应记录游戏版本、模组依赖、玩家数、各端配置和日志。

IL emitter 有独立的 [PickupGrantILRegression](../validation/PickupGrantILRegression/README.md)，链接实际生产 PickupGrantIL、AnimationPrefabIL 和 CommandCubeIL，核查地址加载、局部捕获、回调顺序、Transform 栈保留和失败诊断；指挥精华增加完整有限指令集栈/类型/分支验证与合成 CLR 执行。可选参数检查安装的真实游戏方法结构及当前源码登记的 Release IL 模式，不依赖固定本机路径，带参数的审计须从仓库根目录执行：

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
# 可选：只在内存转换指定真实 DLL，文件不会被修改
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

pickup/动画结构断言不执行注入后的方法；指挥精华执行的只是合成 CLR 方法，均不能替代目标游戏的 IL Hook 安装与运行验证。首次基于旧 DLL 的检查曾发现将结构体值误当作 ref 的注入问题，现已改为在原始加载前发出该局部的地址。

| 场景 | 核对结果 |
| --- | --- |
| 两位玩家，普通箱 InstanceObject | 两人各开一次；克隆物品/价格一致；掉落可共享 |
| 普通箱 InstanceItems | 开一次；每人只能领取一次；最后一人领取后销毁 |
| 普通箱 InstanceBoth / owner-only | 交互与掉落归属符合模式；无资格者不能手动或自动拾取 |
| 指挥精华 / VoidTriple，SharePickupPickers 开/关 | 分别选择或共享选择符合配置；未领取者资格不丢失 |
| 多重商店 / 配送终端 | 主体与终端价格、物品、关闭行为、玩家归属正确 |
| 锁箱 / 虚空锁箱 | 固有拥有者从生成流程传递到开启与输出 |
| Boss / 牺牲 / 拾荒者背包 | 对比平衡开关，检查份数、归属与整数除法边界 |
| 两端交替拾取、迟加入、换关 | 现有实例的客户端显示和资格一致；迟加入不假设自动新增份额 |
| uninstance / uninstanceall | 自己的资格移除、公开掉落出现、其他人的资格保留 |
| 标记、全息价格、分屏相机、虚空材质 | 对应玩家的可用性与显示一致，记录材质限制 |
| 若修改生命周期：启用/禁用、退出重进 | 无重复 Hook、残留组件、重复领取或新的卸载异常 |

提交前检查 `git diff --check` 和 `git status --short`，避免加入生成文件。`bin/`、`obj/`、`Release/` 已被忽略；根目录 ZIP 不一定被忽略。

每次关键修改完成并完成相应验证后，必须创建 Git commit。功能新增、缺陷修复、架构/配置/依赖调整及重要文档更新都属于关键修改；同一目的的配套改动可合为一个提交，不同目的应分别提交。提交前检查暂存区，确保只包含本次任务相关文件；提交后报告 commit hash、修改内容及验证结果。完整协作约束见 [AGENTS.md](../AGENTS.md)。

凡 Codex 参与完成的提交，必须在提交信息末尾添加以下 Git trailer，与前面的提交说明以空行分隔，并在提交后核对：

```text
Co-authored-by: Codex <codex@openai.com>
```
