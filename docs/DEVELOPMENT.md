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

对 droplet 的既有 IL Hook 检查确认其目标已经是 `CreatePickupDroplet(CreatePickupInfo, Vector3, Vector3)`，并未 Hook 废弃的 PickupIndex 重载；普通物品 `CreatePickup(in CreatePickupInfo)` 也仍为当前签名。旧真实 DLL 证明各 droplet 重载汇合到该入口、落地和强制初始化均进入 `CreatePickup()`，因此来源上下文改包围 CreatePickup 而非仅 OnCollisionEnter，涵盖强制初始化。旧 DLL 的 AttemptGrant 两次读取 GrantContext.shouldDestroy（设置 consumed、最终销毁）；原补丁只改第一次读取的返回值。本次改为在首次读取前按引用更新整个 GrantContext，让成功领取且尚有资格者时两处判断一致保留对象；授予失败时不移除资格。新增成对注册/卸载的 On.AttemptGrant 在授予前检查资格，阻止直接调用绕过交互/UI 检查；资格与领取结果逻辑集中在不安装 Hook 的 PickupGrantPolicy 辅助类，供隔离回归直接链接。1.5.0 的同名字段与方法签名已核对，真实方法体与 IL 匹配仍待该版游戏验证。

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

本次 1.5.0 适配记录（基于 `1127e98`，2026-10-15）：`dotnet restore RoR2Mods.sln --nologo` 成功；最终 Release `--no-restore` 构建 **0 错误、1 警告**（SceneDirectorHandler 的旧 Inventory.GetItemCount API）。首次构建暴露的轮盘箱 UniquePickup 签名 4 个错误已修复。隔离 MultiplayerRegression **33 个断言通过**；PickupGrantILRegression 默认合成方法 **15 个结构断言通过**，另对本机旧真实 DLL 检查 **7 个结构断言通过**，合计 22。`git diff --check` 通过。未执行打包、上传或发布，也未执行游戏内测试。

GameLibs 1.5.0-r.1 内的方法体为 `throw null` 的编译桩，反编译仅用于核对字段、事件和方法签名，不能证明 IL 游标或运行行为兼容。本机 Steam 安装为 build `21587608`，待更新目标为 `25475991`；已有游戏程序集的静态分析属于旧安装证据，不能当作 1.5.0 运行验证。尚须在更新后的真实游戏中检查所有 IL Hook 安装、两端联机、分屏和下表场景。

## 打包与版本

```powershell
./build-release.ps1
# 参数可选；正常发布保留 Release
./build-release.ps1 -Configuration Release
```

脚本把工作目录设为自身目录，删除并重建根目录 `Release/`，复制元数据，执行构建并复制 DLL，再根据 manifest 版本压缩。它会覆盖同版本 ZIP。当前产物结构为：

```text
Nicebowl-InstancedLoot-1.5.0.zip
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
| `InstancedLoot.cs` 的 BepInPlugin 属性 | `1.5.0`，插件元数据 |
| `manifest.json` | `1.5.0`，发布包版本及 ZIP 命名依据 |
| `InstancedLoot.csproj` | `1.5.0`，项目/程序集构建版本元数据 |
| `CHANGELOG.md` | 最近记录 `1.4.0`，声明 Alloyed Collective DLC 支持 |

本次适配明确统一三个版本为 1.5.0，保留原 GUID。CHANGELOG 的 1.4.0 为历史发布记录，本次尚未发布；历史兼容性声明不能替代具体对象注册检查和游戏实测。

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

IL emitter 有独立的 [PickupGrantILRegression](../validation/PickupGrantILRegression/README.md)，链接实际生产 PickupGrantIL，使用真实 MonoMod/Cecil 核查合成 IL 的地址加载、局部捕获、回调顺序与失败诊断；可选参数检查安装的真实游戏方法结构，不依赖固定本机路径：

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
# 可选：只在内存转换指定真实 DLL，文件不会被修改
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

结构断言不执行注入后的方法，不能替代目标游戏的 IL Hook 安装与运行验证；首次基于旧 DLL 的检查曾发现将结构体值误当作 ref 的注入问题，现已改为在原始加载前发出该局部的地址。

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
