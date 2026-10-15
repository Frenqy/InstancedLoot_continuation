# 开发、构建与验证

## 开发环境

项目是 SDK 风格 C# 类库，目标 `netstandard2.1`，`LangVersion=default`。源码使用 C# 12 集合表达式，例如 `[..players]`，因此编译器需要支持 C# 12；可从 .NET 8 或更新 SDK 开始。仓库没有 `global.json` 锁定 SDK。目标框架是游戏中加载程序集的兼容目标，不代表必须安装同版本的现代 .NET 运行时才能玩模组。

本次实际使用 Windows PowerShell 和 **.NET SDK 10.0.201** 完成 Release 构建。编译无需打开 Unity Editor；运行验证需要安装游戏、BepInEx 及运行依赖。

## 编译依赖与运行依赖

项目文件中显式声明的 NuGet 包：

| 包 | 版本 |
| --- | --- |
| BepInEx.Core | `5.*`，浮动版本 |
| RiskOfRain2.GameLibs | `1.4.0-r.0` |
| UnityEngine.Modules | `2021.3.33` |
| MMHOOK.RoR2 | `2024.12.10`，该引用抑制 NU1701 |
| R2API.Core | `5.1.4` |
| R2API.Networking | `1.0.3` |

`nuget.config` 添加 `https://nuget.bepinex.dev/v3/index.json` 与 `https://api.nuget.org/v3/index.json`；不是包锁文件，也没有清空上层可能存在的其他源。

`manifest.json` 的运行依赖是 `bbepis-BepInExPack-5.4.2117`、`RiskofThunder-HookGenPatcher-1.2.5`、`RiskofThunder-R2API_Core-5.1.4`、`RiskofThunder-R2API_Networking-1.0.3`。它们与 NuGet 编译引用用途不同，更新时分别核对。

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

日期：2026-10-15。代码基线：`46427bb`；本次仅新增/修改文档。

执行 `dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --nologo`，包含还原，**构建成功，0 错误、4 警告**：

| 警告 | 位置 | 内容 |
| --- | --- | --- |
| CS0618 | `Hooks/SceneDirectorHandler.cs` | Inventory.GetItemCount(ItemDef) 已过时 |
| CS0618 | `Hooks/PickupPickerControllerHandler.cs` | onPickupSelected 已过时 |
| CS0618 | `InstancedLoot.cs` | GenericPickupController.pickupIndex 已过时 |
| CS0414 | `Components/FadeBehavior.cs` | lastCameraRigController 赋值后未使用 |

未运行游戏、联机/分屏测试或发布打包脚本。此记录仅证明该环境下的编译和依赖还原可完成，未证明运行时 IL 匹配适配当前已安装游戏。

## 打包与版本

```powershell
./build-release.ps1
# 参数可选；正常发布保留 Release
./build-release.ps1 -Configuration Release
```

脚本把工作目录设为自身目录，删除并重建根目录 `Release/`，复制元数据，执行构建并复制 DLL，再根据 manifest 版本压缩。它会覆盖同版本 ZIP。当前产物结构为：

```text
Nicebowl-InstancedLoot-1.4.0.zip
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
| `InstancedLoot.cs` 的 BepInPlugin 属性 | `1.4.0`，插件元数据 |
| `manifest.json` | `1.4.0`，发布包版本及 ZIP 命名依据 |
| `InstancedLoot.csproj` | `3.0.0`，项目/程序集构建版本元数据 |
| `CHANGELOG.md` | 最近记录 `1.4.0`，声明 Alloyed Collective DLC 支持 |

发布前显式决定并核对版本，不在普通文档或功能修复任务中自动统一它们。Changelog 的兼容性声明不能替代具体对象注册检查和游戏实测。

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

仓库没有独立测试项目或 CI；`dotnet test` 不能代替游戏内验证。文档变更检查路径、符号和 diff；代码变更先 Release 构建，再按影响范围选择以下场景。结果应记录游戏版本、模组依赖、玩家数、各端配置和日志。

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
