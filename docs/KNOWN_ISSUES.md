# 已知限制与维护风险

本页区分历史说明、源码确认的实现缺口和需要运行时验证的疑点。核对日期 2026-10-15；本次文档整理没有修复这些逻辑，也没有复现游戏内症状。

## 用户侧限制

- 主机与客户端都需要安装模组；网络协议没有配置文件同步。
- 局中改变模式不会重建当前场景全部实例；原 README 提示当前关卡可能掉落过多。
- 装备拾取涉及交换，未进入普通物品独立领取路径；无人机/炮塔尚未实现。
- 某些材质不支持 DITHER/_Fade，特别是虚空相关显示；效果实例化也没有覆盖所有系统。
- 其他模组的交互物需要明确接入来源识别与处理器注册。
- 原 README 声称支持 XSplitScreen；代码确有按相机玩家处理显示的设计，本次未验证当前分屏兼容性。
- 原 README 描述聊天命令适用于选择器，但实际放弃逻辑依赖 CreatePickupInfoTracker，不能保证所有生成路径都有效。

## 源码直接可见的问题和待核实风险

| 位置 | 静态证据 | 影响或后续核实方向 |
| --- | --- | --- |
| `Hooks/ChatHandler.cs` 的 UnregisterHooks | 使用 `+= On_Chat_CCSay`，不是 `-=` | 卸载方法会增加订阅；最终是否残留还受插件的 RemoveAllOwnedBy 清理影响，应验证启停流程 |
| `Hooks/SacrificeArtifactManagerHandler.cs` 的 UnregisterHooks | On 钩子使用 `-=`，IL 钩子仍使用 `+=` | 同样存在卸载不对称，不能认为所有 Hook 已干净卸载 |
| `Networking/SyncInstances.cs` 的 HandleMessageInternal | 来源对象解析后使用 `validated = validated && sourceObject == null` | 校验方向与“等待来源对象存在”的目标不一致，可能接受尚不存在的来源；需要有延迟的多人场景验证后修复 |
| `SyncInstances` / `InstancedLoot.FailedSyncs` | 超过重试阈值后只保存失败条目，未发现重放逻辑 | 不应把失败列表当作自动恢复队列；排查时同时核对 target、player、source 的生成时序 |
| `AbstractObjectHandler.CloneObject` | 回退查找只移除字符串 `(Cloned)` | 与常见 Unity `(Clone)` 命名不同；有 SpawnCardTracker 时走首选路径，回退失败要核对实际名称 |
| `InstanceInfoTracker.InstanceOverrideInfo.AttachTo` | 已有 Tracker 时不更新 Owner | 传入新 Owner 不保证覆盖已有组件；应显式检查 SetOwner 的调用 |
| 多个掉落 Hook、AnimationEventsHandler | 设置全局/Handler 临时上下文，调用 orig 后置 null，无 finally | 原调用抛异常或嵌套时可能遗留/丢失来源；新增路径应恢复旧值，现有路径待专项修复 |
| `BossGroupHandler.On_BossGroup_Start` | 替代实现没有调用 orig(self) | 是否丢失当前游戏版本的初始化行为需核对游戏实现；不要据此直接假定所有 Boss 掉落已坏 |
| `Config.GetValidPlayersSet`、`BossGroupHandler` | 前者直接使用 instances；后者按 participatingPlayerCount 取模再索引 instances | 断线/人数修正模组可能导致计数与顺序不一致，入口 TODO 也提到此问题 |
| `Components/InstanceHandler.cs` 的 OnDestroy | 从静态 Instances 删除，但未显式从 SharedInfo.LinkedHandlers 删除 | 若组内成员单独销毁，需检查共享引用与后续同步；未实测是否出现故障 |
| `Components/PingerControllerRenderBehaviour.cs` | 目标 Renderer 的实际 highlight 赋值被注释 | 当前存在显示过滤，不等于完整的高亮目标重定向 |
| `Configuration/Config.cs` | Ready 恒 true，迁移表当前为空 | 不要依赖它作为异步配置初始化完成信号；新增迁移需重新核对绑定时机 |

## 未注册或识别不完整

`LunarShopHandler` 和 `InteractorHandler` 未启用；TripleShopLarge 从类型表注释掉；VoidFields、PickupLunarCoin 没有注册处理器。锅和净化池虽然登记到 PrinterHandler，但缺少当前名称识别路径。具体映射见 [代码导航](CODE_MAP.md)。

## 历史 TODO，不等于已确认缺陷

`InstancedLoot.cs` 顶部仍保留若干历史记录：箱子增加 PickupPickerController 后的临时适配、月球祭坛、虚空选择异常、传送器视觉、断线玩家、丢弃物显示、放弃操作专用同步等。部分 TODO 后又写“似乎修好”，不能仅凭这些注释判断当前版本结果。

`ReduceInteractibleBudget`、`ReduceSacrificeSpawnChance`、`ReduceMoneyDrops` 的配置描述仍标记未测试；不要在文档中把它们称为经过验证的平衡方案。

## 构建与发布注意项

- 插件和 manifest 为 1.4.0，项目 Version 为 3.0.0；发布前需要明确版本策略。
- BepInEx.Core 使用浮动 `5.*`，仓库没有 SDK 或包锁文件；未来还原结果可能不同。
- Debug 包会启用改变网络行为的开发 Hook；发布流程应使用 Release。
- 打包脚本硬编码 ZIP 前缀，且会删除 Release 目录、覆盖同版本 ZIP。
- 打包脚本没有复制 LICENSE；仓库采用 MIT License，应在正式分发流程中核对和保留其要求的许可文本。
- 本次 Release 编译通过但有 4 个现有警告，详见 [验证记录](DEVELOPMENT.md)。无游戏内测试结果，不代表最新游戏运行时已验证兼容。
