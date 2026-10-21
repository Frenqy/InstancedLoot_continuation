# AI 协作指南

适用范围：整个仓库。先读本文件和 [文档索引](docs/README.md)，再读 [架构](docs/ARCHITECTURE.md)，按任务查 [代码导航](docs/CODE_MAP.md)、[已知问题](docs/KNOWN_ISSUES.md)。配置任务读配置文档；构建和扩展读开发文档。

## 项目速览

- 这是《Risk of Rain 2》的 BepInEx 5 模组，不是 Unity Editor 工程或独立应用；主机与客户端需要同版本。
- 唯一项目 `InstancedLoot/InstancedLoot.csproj`，解决方案 `RoR2Mods.sln`，目标 netstandard2.1。validation 是隔离验证程序，不进模组 DLL。
- 入口 `InstancedLoot/InstancedLoot.cs`，GUID 保持 `com.kuberoot.instancedloot`，配置文件沿用该 GUID。
- 当前模组版本 **2.0.0**（插件、csproj、manifest 一致），目标仍是 Hallowed Concepts / 游戏 1.5.0。依赖通过 NuGet，不复制游戏 DLL 进仓库。
- 2.0 只实现 PreferredInstanceMode=InstanceObject：每位生成时在场的玩家有一个独立原生购买物，物品掉落公开。旧 InstanceItems/owner-only/preset/alias/chat 路径已移除，历史见 Git 和 HISTORY_1_X。
- 文档基于 2026-10-16 核对。编译和 stub 不证明 Unity/Mono Hook 安装或多人实测有效。

## 必须保留的设计约束

- ObjectInstanceService 管服务器生命周期，ObjectAdapter 捕获/恢复原生状态，InstanceHandler.Owner 是单一固定归属。不要再把用户 InstanceObject 策略等同于一个共享资格对象。
- 初始化/复制/清理必须 server-only；客户端显示与快照解析必须可执行。权限不能由视觉隐藏代替。
- 源原生初始化一次，副本通过 SpawnCard/Awake 后由 Start 接管；不要直接 Instantiate，也不要让副本重跑随机初始化。捕获闭包不依赖源后续存活。
- 复制最终价格、成本类型、available、RNG、商品及子物状态，网络字段用 SyncVar setter。副本后续价格和开关独立。
- MultiShop.Awake 已建终端，绝不再 CreateTerminals；源等所有终端 hasStarted，再捕获。副本终端先 Start 也要拦截。所有终端都有归属，根/对应槽位的 groupId 只用于显示关联。
- 购买由原生 PayCost/onPurchase/关闭/冷却负责。受管对象只做归属与重入门禁，不能把 available=false 视为永久消费，不能强制关闭 Executive Card 保留的终端。未受管对象（尤其打印机）的购买和候选必须原生透传。
- source pending 必须封锁全部关联购买子物；复制失败回滚已生成根与独立网络子物并让源回退原版。副本初始化失败不得留下无主可购买物。
- 修理支持针对损坏待修复购买物，不是活动 CharacterBody；保留 masterPrefab、网络升级数和原生 activator 召唤归属。未知/全局神龛不宣称隔离。
- OwnerSnapshot 是协议 v2 的单实体消息。字段变化同时核对两端、版本、session/epoch/revision、乱序、延迟和 netId 复用测试；不能重新依赖 source/整组解析。
- pending 超时、场景/Run 切换和卸载要清理临时标记；保留未来 session 提前到达的消息，拒绝旧 epoch。周期重发不得给迟加入凭空增加份额。
- 客户端候选覆盖 raycast 与 overlap；受管对象要处理多个 IInteractable、禁用组件，实际服务器 PerformInteraction 同样有归属检查。
- 按每个相机的玩家显示，保留分屏；Renderer.forceRenderingOff 不覆盖原生 enabled。PostRender 恢复暂存状态，父子 view 不可重复管理同一 renderer/effect。
- 新 Hook 成对注册/卸载并进入 HookManager；新增对象同时补适配器、识别、配置、网络/显示与验证。仅有常量或空类不表示支持。

## 常用命令

在仓库根目录使用 PowerShell：

```powershell
dotnet restore RoR2Mods.sln
dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --no-restore
git diff --check
```

打包命令为 `./build-release.ps1`。该脚本会删除并重建根目录 `Release/`，覆盖同版本 ZIP；只有需要打包时才运行。详细输出位置和验证记录见 [开发文档](docs/DEVELOPMENT.md)。

## 工作约定

- 每个后续修改批次/交付都必须递增模组版本，包括仅修改文档或协作规则的任务。同一任务的配套改动统一递增一次；版本号、CHANGELOG 和文档同步属于该批次，不再触发额外递增，不要求每次临时编辑或同一批次内的提交分别升级。默认递增 patch；新增功能递增 minor，破坏兼容性递增 major。同步修改 `InstancedLoot/InstancedLoot.cs` 的 BepInPlugin 版本、`InstancedLoot/InstancedLoot.csproj` 的 Version、`manifest.json` 的 version_number，并更新 `CHANGELOG.md`、[开发文档版本表](docs/DEVELOPMENT.md#打包与版本) 和其他当前版本描述；保留历史版本及验证事实。目标游戏版本和依赖版本独立判断，不随模组版本机械替换。
- 每次关键修改完成并完成相应验证后，必须创建 Git commit，不得仅将改动留在工作区。关键修改包括功能新增、缺陷修复、架构/配置/依赖调整，以及重要文档或协作约束更新；同一目的的配套改动可合为一个提交，不同目的的关键修改应分别提交。
- 提交前检查 diff 和暂存区，只纳入本次任务相关改动；提交信息应概括修改目的。完成后向用户报告 commit hash、主要内容与验证结果；若提交受阻，明确说明原因，不得声称已提交。
- 凡 Codex 参与完成的提交，必须在提交信息末尾添加 Git trailer：`Co-authored-by: Codex <codex@openai.com>`，并与前面的提交说明以空行分隔。提交后核对该署名已写入 commit。
- 沿用现有 C# 风格：文件范围命名空间、按功能分 Handler、四空格缩进；避免为单个修复重排全部文件。
- 默认用 Release 验证。2.0 已移除旧 `DevelopmentHooksHandler`；不得重新引入修改联机连接或消息哈希的开发行为。
- `validation/MultiplayerRegression` 提供链接生产源码的隔离 stub 回归；没有 Unity 或自动化联机测试、CI 配置。按改动执行 Release 构建、相关回归和游戏内验证，不要把 stub 通过描述成联机实测。
- 文档类任务不顺手修改游戏逻辑或依赖；仍按上述规则递增模组版本并维护当前 CHANGELOG，保留历史记录事实。发现问题记录证据，功能修复另按任务范围处理。
- 不提交 `bin/`、`obj/`、`Release/`、本机配置、日志或 NuGet 缓存；根目录生成的 ZIP 也不要意外加入提交。
- 修改行为、配置、依赖、注册关系时更新相应文档。事实以当前代码为准；区分“源码行为”“历史 TODO”“已实测结果”。
