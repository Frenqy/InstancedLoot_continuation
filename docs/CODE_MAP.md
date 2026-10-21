# 2.0 代码导航与支持边界

所有路径相对仓库根目录。旧 HandleInstancing/ObjectHandlerManager/SharedInfo、物品共享和 owner-only 源码已移除；历史在 Git 及 [HISTORY_1_X.md](HISTORY_1_X.md)。

| 文件 | 当前职责 |
| --- | --- |
| InstancedLoot/InstancedLoot.cs | 插件生命周期、Run/场景清理、每两秒归属重发、玩家加入补发 |
| InstancedLoot/ObjectInstancing/ObjectInstanceService.cs | 服务器源排队、SpawnCard 复制、不可变快照、待初始化副本与失败回滚 |
| InstancedLoot/ObjectInstancing/ObjectAdapter.cs | Purchase 成本/类型/available/RNG 的快照边界 |
| InstancedLoot/ObjectInstancing/NativeAdapters.cs | 箱、选择箱、轮盘、商店、三类神龛与修理物适配 |
| InstancedLoot/Hooks/ObjectLifecycleHooks.cs | 成对 Hook 各组件 Start；接管副本初始化和损坏修理物识别 |
| InstancedLoot/Components/InstanceHandler.cs | 固定 Owner、groupId、revision；显示关联 |
| InstancedLoot/Components/PendingOwnership.cs | 初始化/归属解析期间的交互与显示封锁 |
| InstancedLoot/Components/InstanceAccessPolicy.cs | Interactor/Body 到玩家的解析与权限 |
| InstancedLoot/Hooks/PurchaseInteractionHandler.cs | 受管购买权限与事务重入；未受管原生透传 |
| InstancedLoot/Hooks/InteractorHandler.cs、InteractorCandidateIL.cs | raycast/overlap、多 IInteractable 候选及服务器实际交互权限 |
| InstancedLoot/Networking/OwnerSnapshot.cs | 新版单实体消息、session/epoch/revision、延迟解析与清理 |
| InstancedLoot/Configuration/Config.cs、Enums.cs | 唯一 InstanceObject 策略、逐类布尔开关、两项平衡配置 |
| InstancedLoot/Components/FadeBehavior.cs | 分屏逐相机模型/效果过滤与 PostRender 恢复 |
| InstancedLoot/Hooks/DitherModelHandler.cs、HologramProjectorHandler.cs | 原生 dither 与价格提示适配 |
| InstancedLoot/Hooks/PingHandler.cs、Components/PingerControllerRenderBehaviour.cs、Utils.cs | 对应玩家副本的标记可见性 |
| InstancedLoot/Hooks/AnimationEventsHandler.cs、AnimationPrefabIL.cs、EffectManagerHandler.cs | 动画及非池化效果归属显示 |
| InstancedLoot/Hooks/SpawnCardHandler.cs | 保存实际 SpawnCard，避免只靠名称搜索 |
| InstancedLoot/Hooks/SceneDirectorHandler.cs、DeathRewardsHandler.cs | 可选生成预算与怪物金币缩减 |

## 支持表（源码接入，尚无 2.0 游戏实测）

| 类型 | 当前处理 |
| --- | --- |
| Chest1/Chest2/GoldChest/Chest1StealthedVariant、六种 CategoryChest | 每玩家原生箱，商品/价格/RNG 快照 |
| EquipmentBarrel、LunarChest、VoidChest、Lockbox、ScavBackpack | Chest 适配；普通掉落公开 |
| VoidTriple、LockboxVoid | 每玩家选择箱；选择列表独立 |
| CasinoChest | 每玩家轮盘箱 |
| TripleShop、TripleShopEquipment、FreeChestMultiShop | 每玩家一套原生 controller+terminal；不重复生成终端 |
| ShrineChance、ShrineBlood、ShrineRestack | 三个独立开关；保留原生各玩家刷新/使用次数 |
| Drone、Turret | 损坏待修复购买物；需可用 SpawnCard，保留原生召唤归属 |
| 打印机、炼金锅、净化池、Scrapper、月球商店、未知/全局神龛 | 原版，无对象/掉落实例化 Hook |
| 普通掉落、Boss、牺牲掉落、Command essence | 原版拾取/选择，无共享物品资格 |

修理物识别要求 Broken 名称与 Drone/Turret 字样，同时具备 PurchaseInteraction、有效 masterPrefab 及 destroyAfterSummoning；无 card 的特殊 BrokenBody 记录错误并整源回退原版。TripleShopLarge、DLC 新交互物及第三方对象没有被名称前缀误认为已支持。

验证入口是 [MultiplayerRegression](../validation/MultiplayerRegression/README.md) 与 [IL/lifecycle audit](../validation/PickupGrantILRegression/README.md)。
