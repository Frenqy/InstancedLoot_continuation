# 已知限制与维护风险

本页区分历史说明、源码确认的实现缺口和需要运行时验证的疑点。核对日期 2026-10-15；多人修复已通过构建与 stub 回归，未复现双端游戏内症状。

## 用户侧限制

- 主机与客户端都需要安装模组；网络协议没有配置文件同步。
- 局中改变模式不会重建当前场景全部实例；原 README 提示当前关卡可能掉落过多。
- 装备拾取涉及交换，未进入普通物品独立领取路径；无人机/炮塔尚未实现。
- 某些材质不支持 DITHER/_Fade，特别是虚空相关显示；效果实例化也没有覆盖所有系统。
- 其他模组的交互物需要明确接入来源识别与处理器注册。
- 原 README 声称支持 XSplitScreen；代码确有按相机玩家处理显示的设计，本次未验证当前分屏兼容性。
- 原 README 描述聊天命令适用于选择器，但实际放弃逻辑依赖 CreatePickupInfoTracker，不能保证所有生成路径都有效。

## 源码直接可见的问题和待核实风险

2026-10-15 的多人修复覆盖以下静态问题，不能据此将关联 GitHub issue 视为已通过游戏实测：

- #3/#5/#6 的低价报告：CopyObject 副本现在统一复制源对象最终购买成本，并通过原生网络属性同步。旧实际游戏 DLL 显示 SceneDirector 在 SpawnCard 后设置金币难度价格，副本此前绕过该步骤。#5 还报告 InstanceItems，单独该模式不产生交互物副本，仍须独立复现。
- #3/#6/#8 的重叠显示报告：修正 source 存在性校验、超过 40 帧即放弃、旧等待快照覆盖新资格、销毁成员残留与 dither 资格缓存。它们是可能影响客户端归属/显示的确定源码问题；Swarms、host-only、特定关卡仍需分别验证，不能断言全部同源。
- 增加服务器购买入口资格校验；各副本后续价格仍由原生祭坛/商店逻辑独立更新，没有团队扣款行为。
- #9 的共享掉落路径：普通 pickup 现在在实际授予前检查资格；失败授予保留资格，成功后改原生 GrantContext 本身，防止只拦截 consumed 的第一次读取而仍在第二次读取时销毁。旧实际 DLL 中可见两次 shouldDestroy 读取。droplet 上下文改为覆盖 CreatePickup，包含 ForcePickupInitialization，并在轮盘/选择器/droplet 路径用 finally 恢复。目标新 IL、Command/Sacrifice/Swarms 组合仍需游戏矩阵验证。
- [回归执行器](../validation/MultiplayerRegression/README.md) 直接链接生产源码，以替身控制对象生成/销毁和协程时序。它不运行游戏 IL、真实网络、SyncVar 脏位或实际渲染。最新构建与未测边界见 [开发文档](DEVELOPMENT.md)。

同步仍有限制：超过 30 秒的缺失引用会超时并仅保存诊断，不会后台重放；按本地接收顺序淘汰旧 pending，不识别网络包本身乱序；同一 Run 的换关没有专用取消事件；迟加入不会新增副本或份额。

| 位置 | 静态证据 | 影响或后续核实方向 |
| --- | --- | --- |
| `Hooks/ChatHandler.cs` 的 UnregisterHooks | 使用 `+= On_Chat_CCSay`，不是 `-=` | 卸载方法会增加订阅；最终是否残留还受插件的 RemoveAllOwnedBy 清理影响，应验证启停流程 |
| `Hooks/SacrificeArtifactManagerHandler.cs` 的 UnregisterHooks | On 钩子使用 `-=`，IL 钩子仍使用 `+=` | 同样存在卸载不对称，不能认为所有 Hook 已干净卸载 |
| `InstanceInfoTracker.InstanceOverrideInfo.AttachTo` | 已有 Tracker 时不更新 Owner | 传入新 Owner 不保证覆盖已有组件；应显式检查 SetOwner 的调用 |
| 多个掉落 Hook、AnimationEventsHandler | 设置全局/Handler 临时上下文，调用 orig 后置 null，无 finally | 原调用抛异常或嵌套时可能遗留/丢失来源；新增路径应恢复旧值，现有路径待专项修复 |
| `BossGroupHandler.On_BossGroup_Start` | 替代实现没有调用 orig(self) | 是否丢失当前游戏版本的初始化行为需核对游戏实现；不要据此直接假定所有 Boss 掉落已坏 |
| `Config.GetValidPlayersSet`、`BossGroupHandler` | 前者直接使用 instances；后者按 participatingPlayerCount 取模再索引 instances | 断线/人数修正模组可能导致计数与顺序不一致，入口 TODO 也提到此问题 |
| `Components/PingerControllerRenderBehaviour.cs` | 目标 Renderer 的实际 highlight 赋值被注释 | 当前存在显示过滤，不等于完整的高亮目标重定向 |
| `Configuration/Config.cs` | Ready 恒 true，迁移表当前为空 | 不要依赖它作为异步配置初始化完成信号；新增迁移需重新核对绑定时机 |

## 未注册或识别不完整

`LunarShopHandler` 和 `InteractorHandler` 未启用；TripleShopLarge 从类型表注释掉；VoidFields、PickupLunarCoin 没有注册处理器。锅和净化池虽然登记到 PrinterHandler，但缺少当前名称识别路径。具体映射见 [代码导航](CODE_MAP.md)。

## 历史 TODO，不等于已确认缺陷

`InstancedLoot.cs` 顶部仍保留若干历史记录：箱子增加 PickupPickerController 后的临时适配、月球祭坛、虚空选择异常、传送器视觉、断线玩家、丢弃物显示、放弃操作专用同步等。部分 TODO 后又写“似乎修好”，不能仅凭这些注释判断当前版本结果。

`ReduceInteractibleBudget`、`ReduceSacrificeSpawnChance`、`ReduceMoneyDrops` 的配置描述仍标记未测试；不要在文档中把它们称为经过验证的平衡方案。

## 构建与发布注意项

- 插件、manifest 与项目版本现为 1.5.0，BepInEx.Core 已固定；仓库仍没有 SDK 或包锁文件，完整依赖版本见开发文档。
- Debug 包会启用改变网络行为的开发 Hook；发布流程应使用 Release。
- 打包脚本硬编码 ZIP 前缀，且会删除 Release 目录、覆盖同版本 ZIP。
- 打包脚本没有复制 LICENSE；仓库采用 MIT License，应在正式分发流程中核对和保留其要求的许可文本。
- 本次 Release 编译和 stub 回归通过，详见 [验证记录](DEVELOPMENT.md)。本机实际游戏 DLL 为待更新的旧版本；没有双端游戏内测试结果，不代表最新游戏运行时已验证兼容。
