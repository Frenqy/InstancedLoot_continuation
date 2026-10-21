# 2.0 开发、构建与验证

## 环境与依赖

项目是 netstandard2.1 BepInEx 5 类库；源码使用现代 C#。本批使用 Windows PowerShell 与 .NET SDK **10.0.201**，不需要 Unity Editor。

| 编译包 | 固定版本 |
| --- | --- |
| BepInEx.Core | 5.4.21 |
| RiskOfRain2.GameLibs | 1.5.0-r.1 |
| UnityEngine.Modules | 2021.3.33 |
| MMHOOK.RoR2 | 2026.10.8 |
| R2API.Core | 5.3.0 |
| R2API.Networking | 1.0.4 |

manifest 的运行依赖是 BepInExPack 5.4.2122、HookGenPatcher 1.2.9、RoR2BepInExPack 1.46.0、R2API_Core 5.3.0、R2API_Networking 1.0.4。NuGet 编译版本与运行整合包不是同一版本概念。目标仍是 2026-10-08 Hallowed Concepts / 游戏 1.5.0；2.0 没有机械升级游戏/依赖。此前依赖来源与历史验证见 [HISTORY_1_X.md](HISTORY_1_X.md)。

## 构建与验证

从仓库根目录运行：

```powershell
dotnet restore RoR2Mods.sln
dotnet build InstancedLoot/InstancedLoot.csproj --configuration Release --no-restore
dotnet run --project validation/MultiplayerRegression --configuration Release
dotnet run --project validation/PickupGrantILRegression --configuration Release
# 可选：真实安装的未剥离游戏 DLL，只在内存读取和变换
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
git diff --check
```

默认验证 Release；旧 Debug DevelopmentHooksHandler 已移除。主 DLL 在 InstancedLoot/bin/Release/netstandard2.1/，validation 不进模组 DLL，不提交任何生成目录。

## 2.0.0 验证记录（2026-10-16）

| 验证 | 本批结果与证明范围 |
| --- | --- |
| Release 构建 | 0 错误、0 警告，程序集版本 2.0.0.0 |
| MultiplayerRegression | **82** 条断言；直接链接新 Service、Adapters、OwnerSnapshot、Lifecycle、Purchase、Interactor、Fade、Ping/Utils |
| 新 candidate emitter | 实际执行合成 CLR 的 raycast/overlap 两路径，包含 incoming branch 与 out IInteractable；缺失匹配负例显式失败 |
| 真实 DLL candidate emitter | 实际变换两个 TryGetComponent 入口，核对 callback 参数与无关分支；不执行游戏方法 |
| 当前登记 Release IL 审计 | **7** 个模式通过，登记缺漏会失败；除明确的生产 emitter 外为匹配检查 |
| 原生生命周期/金币审计 | **10** 条方法体断言通过，事实列于下方 |
| 历史 IL 回归 | 历史 pickup 15 个合成+7 个安装结构断言、Command cube 旧注入负例与6个 CLR 分支等保留；位于 validation/Historical，**不由 2.0 安装** |
| 生产 AnimationPrefabIL | 4 个合成/真实返回点及4个变化负例通过，无游戏执行 |

本机输入为游戏 1.5.0 / Steam build **25921972** 的 RoR2.dll，SHA256：

```text
48F2758CCCBE1A5EFBA88CFFE0A68631953F489A71A92726572C15A4C57FF90C
```

stub 覆盖两人独立消费与源提前销毁、不可变商品/成本/RNG、三选一一组终端与 Start 顺序、原生 Executive Card 结果、跨终端重入、缺卡回退、部分生成异常回滚、失败副本销毁、独立终端清理、修理升级/activator、三类神龛快照、未受管打印机透传、多组件与两种候选、分屏/嵌套 view 恢复、协议 roundtrip/乱序/早于 Run/stage/scene/owner/销毁/超时/epoch/netId 复用。

原生方法体确认：

- MultiShop.Awake 调一次 CreateTerminals，Start 不再生成终端，只设置 available/cost 与后续显示。
- ShopTerminal.Start 设置 hasStarted，并可能 selfGeneratePickup；捕获前必须等待，副本不能重抽。
- SummonMasterBehavior 将 activator.gameObject 写入 summonerBodyObject，原生 Perform 后按 destroyAfterSummoning 销毁修理物。Start 只补升级显示；升级复制使用 NetworkdroneUpgradeCount setter。
- 金币 PayCost 使用 activatorMaster.money，并保留 MultiShopCardUtils.OnMoneyPurchase；没有改为 GiveTeamMoney，购买事件仍由原生执行。
- Blood 给 activator.master.GiveMoney；Order 调购买者 inventory.ShrineRestackInventory，并非遍历全队。
- 原生 PerformInteraction 遍历所有 IInteractable，所以先做整体门禁后保留合法多组件执行。

**未完成：**Unity/Mono 内安装这批 Hook、原生状态机、真实 SyncVar/R2API 传输、主机/客户端联机、分屏及道具组合。静态检查不能替代游戏实测或证明其他 build 兼容。仓库没有 CI/自动化双游戏测试。旧记录在历史页，不算 2.0 游戏结果。

## 打包与版本

| 位置 | 当前版本 |
| --- | --- |
| InstancedLoot.cs / BepInPlugin | 2.0.0 |
| InstancedLoot.csproj / Version | 2.0.0 |
| manifest.json / version_number | 2.0.0 |
| CHANGELOG.md | 2.0.0 (Unreleased) |
| 构建程序集 | 2.0.0.0 |

本批删除旧模式/协议/集成接口，使用 major **2.0.0** 并保留 GUID。游戏和依赖版本独立。打包入口仍是 build-release.ps1，会重建 Release/ 并覆盖同版本包；**本批没有运行，也未操作任何 ZIP**，只有用户需要包时才运行。

后续每批交付（含文档）须递增版本，同一目的配套变更统一一次；同步版本、CHANGELOG、此表和 AGENTS。关键修改验证后须 Git commit，只暂存任务相关文件，末尾带空行分隔的 `Co-authored-by: Codex <codex@openai.com>` 并核对。完整规则见 AGENTS。

## 新增对象

1. 读真实游戏初始化、购买/销毁与子物方法体，确认 SpawnCard 和效果范围，排除全局对象与未知供应商。
2. 实现 ObjectAdapter 的 IsReady、CaptureState、OwnedObjects；复制值和独立集合，闭包不依赖 source 存活。
3. 在 ObjectLifecycleHooks 成对注册并精确识别；副本先 InitializeCopy，源原生初始化一次后排队。
4. 在 NativeAdapters.ObjectTypes 加逐类配置，不重新引入旧枚举/SharedInfo/物品共享路径。
5. 增加生产链接的顺序、消费、失败、协议和分屏回归，核对真实 DLL，更新文档并安排游戏实测。

2.0 删除旧 PreConfigInit/配置扩展事件，外部集成需适配服务边界；当前没有承诺稳定公共扩展 API。

## 游戏验证清单（尚未执行）

| 场景 | 应确认 |
| --- | --- |
| 双人普通/装备/虚空/选择/轮盘箱 | 独立支付、初始商品/成本相同，输出公开 |
| 三选一/装备/配送 | 每人一组终端，正常关闭与 Executive Card 合法保留 |
| Chance/Blood/Order 单项开关 | 次数/价格/刷新独立，关闭者原版；Order 仅改购买者 |
| 修理/升级/死亡再损坏 | 随从归购买者、升级同步、无 card 回退 |
| 两端/迟加入/换关/禁用/退出重进 | 无幽灵副本、旧 pending/epoch、重复 Hook |
| 分屏/全息/ping/shader/效果 | 各相机正确，原生状态恢复 |
| 打印机/锅/池/未知神龛/Command/Boss/牺牲 | 原版行为保留 |
