# AI 协作指南

适用范围：整个仓库。本文是后续 AI 的首读入口；先建立下面的项目模型，再按任务阅读相关文档与源码，不必先扫描所有文件。

## 项目速览

- 这是《Risk of Rain 2》的 BepInEx 5 模组，不是可以用 Unity Editor 打开的 Unity 工程，也不是独立应用。
- 目的：让多人游戏中的交互物和掉落物按玩家分别使用、领取，并同步归属和显示效果。主机与客户端均需安装。
- 唯一项目：`InstancedLoot/InstancedLoot.csproj`；解决方案：`RoR2Mods.sln`；目标：`netstandard2.1`。
- 插件入口：`InstancedLoot/InstancedLoot.cs`；GUID 为 `com.kuberoot.instancedloot`，也是配置文件名的基础。不要为 continuation 分支随意改 GUID。
- 当前插件属性和 `manifest.json` 版本均为 `1.4.0`，但 `.csproj` 的 `Version` 是 `3.0.0`。这是现状，不能假设三者已一致。
- 核心依赖为游戏程序集、MMHOOK、BepInEx、R2API.Networking。通过 NuGet 引用，不需要把游戏 DLL 复制进仓库。
- 文档基于 2026-10-15 的源码整理；源码变化后应同步更新对应章节。编译成功不代表游戏中的 IL 钩子和多人行为有效。

## 最小阅读路径

1. 阅读本文件和 [文档索引](docs/README.md)。
2. 阅读 [架构与数据流](docs/ARCHITECTURE.md)，理解对象复制、共享领取状态和同步边界。
3. 按任务查 [代码导航](docs/CODE_MAP.md)，仅打开对应源码；修改前阅读 [已知问题](docs/KNOWN_ISSUES.md) 中相关部分。
4. 改配置读 [配置说明](docs/CONFIGURATION.md)；构建、扩展或游戏升级适配读 [开发与验证](docs/DEVELOPMENT.md)。

## 必须保留的设计约束

- 区分 `InstanceMode`（用户策略）和 `ObjectInstanceMode`（对象实现能力）。`InstanceItems` 常用一个对象记录多个玩家的领取资格，并非给每人克隆一个掉落物。
- `HandleInstancing` 是中心入口，但本身没有统一的服务器保护；调用方负责适当的 `NetworkServer.active` 检查。显示层需要在客户端执行。
- `InstanceInfoTracker` 记录来源/拥有者；`InstanceHandler` 记录当前资格和关联实例。不要用视觉淡化代替交互权限检查。
- 复制交互物依赖 `SpawnCard`、`AwaitedObjects` 和后续 `Start` 回调；不要未经核对就改成直接 `Instantiate`，或让复制体重复执行原生初始化并重新抽取物品。
- 克隆体需要保留原对象的 RNG、物品、价格等状态；具体状态复制在各 `ObjectHandlers` 中。
- 修改玩家集合时优先用 `SetPlayers`、`RemovePlayer`、`AddPlayer`，维护 `SharedInfo` 的聚合数据和同步。使用 `sync: false` 后必须由完整初始化流程完成同步。
- `SharedInfo` 未设置时不能直接调用默认会同步的玩家集合方法；遵循现有初始化顺序。
- owner-only 模式缺少拥有者且没有 `PlayerOverride` 时跳过实例化，避免意外复制物品。
- 掉落来源通过钩子对象上的临时 `InstanceOverrideInfo` 跨调用传递。新代码应保存旧值并用 `try/finally` 恢复，兼顾异常与嵌套；现有代码多数尚未这样处理。
- 新增 Hook 必须成对注册/卸载，并进入 `HookManager` 或已注册 ObjectHandler 的 `Init`。新增对象需同时处理识别、注册、能力限制、配置和掉落上下文；只有常量或类文件不表示已支持。
- `SyncInstances` 是二进制消息；改变字段顺序、数量、枚举值时必须同时检查两端读写及版本兼容。
- 保留基于当前相机对应玩家的显示逻辑，避免破坏分屏。不要把所有客户端显示判断替换为单一“本地玩家”。

## 常用命令

在仓库根目录使用 PowerShell：

```powershell
dotnet restore RoR2Mods.sln
dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --no-restore
git diff --check
```

打包命令为 `./build-release.ps1`。该脚本会删除并重建根目录 `Release/`，覆盖同版本 ZIP；只有需要打包时才运行。详细输出位置和验证记录见 [开发文档](docs/DEVELOPMENT.md)。

## 工作约定

- 每次关键修改完成并完成相应验证后，必须创建 Git commit，不得仅将改动留在工作区。关键修改包括功能新增、缺陷修复、架构/配置/依赖调整，以及重要文档或协作约束更新；同一目的的配套改动可合为一个提交，不同目的的关键修改应分别提交。
- 提交前检查 diff 和暂存区，只纳入本次任务相关改动；提交信息应概括修改目的。完成后向用户报告 commit hash、主要内容与验证结果；若提交受阻，明确说明原因，不得声称已提交。
- 沿用现有 C# 风格：文件范围命名空间、按功能分 Handler、四空格缩进；避免为单个修复重排全部文件。
- 默认用 Release 验证。Debug 会启用改变联机连接和消息哈希行为的 `DevelopmentHooksHandler`，不能视为普通调试符号差异。
- 当前无独立测试项目、自动化联机测试或 CI 配置。按改动执行构建和对应游戏内验证，不要声称未执行的测试通过。
- 文档类任务不顺手修改游戏逻辑、依赖、版本或历史 CHANGELOG；发现问题记录证据，功能修复另按任务范围处理。
- 不提交 `bin/`、`obj/`、`Release/`、本机配置、日志或 NuGet 缓存；根目录生成的 ZIP 也不要意外加入提交。
- 修改行为、配置、依赖、注册关系时更新相应文档。事实以当前代码为准；区分“源码行为”“历史 TODO”“已实测结果”。
