# 代码导航与支持边界

路径均相对仓库根目录。下表用于缩小阅读范围；完整控制流见 [架构文档](ARCHITECTURE.md)。

## 顶层与基础模块

| 文件/目录 | 职责 |
| --- | --- |
| `InstancedLoot/InstancedLoot.cs` | 插件生命周期、`HandleInstancing`、放弃领取、玩家加入时补发状态 |
| `InstancedLoot/Enums.cs` | InstanceMode、ObjectInstanceMode、ObjectType、ObjectAlias 及描述/能力限制属性 |
| `InstancedLoot/HookManager.cs` | 通用 Hook 注册表，按类型去重；Debug 条件注册 |
| `InstancedLoot/ObjectHandlerManager.cs` | 对象类型注册表、复制体等待初始化表 |
| `InstancedLoot/AbstractObjectHandler.cs` | SpawnCard 复制、统一复制最终购买成本/独立 RNG、实例组组装、等待/完成初始化流程 |
| `InstancedLoot/AbstractHookHandler.cs` | Hook 基类，定义注册与卸载接口；命名空间为 `InstancedLoot.Hooks` |
| `InstancedLoot/Utils.cs` | 单副本/整个实例组的交互判断、相机对应玩家、物品总量是否增加 |
| `InstancedLoot/Configuration/Config.cs` | 生成配置、模式能力计算、覆盖优先级、缓存、有效玩家集合 |
| `InstancedLoot/Configuration/DefaultPresets.cs`、`ConfigPreset.cs` | 内置预设及预设查询模型 |
| `InstancedLoot/Configuration/Attributes.cs` | ObjectType/alias 元数据属性 |
| `InstancedLoot/Configuration/AcceptableValuesInstanceMode.cs` | BepInEx 可接受模式列表及非法值回退 |
| `InstancedLoot/Configuration/ConfigMigrator.cs` | 反射访问 BepInEx 未绑定条目；当前迁移表为空 |
| `InstancedLoot/Configuration/ItemTierSet.cs` | ItemTier 集合序列化辅助类；当前无有效配置绑定使用它 |
| `InstancedLoot/Components/` | 来源跟踪、实例状态、显示和标记组件，字段解释见架构文档 |
| `InstancedLoot/Networking/SyncInstances.cs` | 唯一自定义网络消息，实例组状态的序列化与客户端重建 |
| `InstancedLoot/InstancedLoot.csproj`、`nuget.config` | 编译目标、依赖版本、包源 |
| `manifest.json`、`build-release.ps1` | Thunderstore 元数据与本地 ZIP 打包 |
| `icon.png`、`Icon.xcf` | 发布图标和可编辑图像源；不参与插件逻辑 |

## 当前注册的 ObjectHandler

以下都位于 `InstancedLoot/ObjectHandlers/`；“已登记”指在 ObjectHandlerManager 中存在，不等同于所有游戏版本都已实测。

| 处理器 | 已登记的类型 | 实现方式 / 特点 |
| --- | --- | --- |
| `ChestHandler` | Chest1、Chest2、GoldChest、Chest1StealthedVariant；CategoryChestDamage/Healing/Utility 及 CategoryChest2Damage/Healing/Utility；EquipmentBarrel、LunarChest、VoidChest、Lockbox、ScavBackpack | CopyObject；复制 RNG、dropCount、currentPickup；Lockbox 支持固有拥有者 |
| `ShrineChanceHandler` | ShrineChance | CopyObject；复制 RNG 与价格 |
| `RouletteChestHandler` | CasinoChest | CopyObject；复制 RNG |
| `MultiShopHandler` | TripleShop、TripleShopEquipment、FreeChestMultiShop | CopyObject；复制商店和终端、价格、隐藏标志和物品；FreeChestMultiShop 支持固有拥有者 |
| `OptionChestHandler` | VoidTriple、LockboxVoid | CopyObject；复制 RNG 和 generatedPickups；LockboxVoid 支持固有拥有者 |
| `PrinterHandler` | Duplicator、DuplicatorLarge、DuplicatorWild、DuplicatorMilitary；三个 LunarCauldron 类型；ShrineCleanse | None，仅处理掉落实例化；锅/净化池来源识别缺口见下节 |
| `ScrapperHandler` | Scrapper | None，仅处理输出的废料 |
| `SpecialItemHandler` | Sacrifice、TrophyHuntersTricorn、BossGroup、TeleporterBoss、SuperRoboBallEncounter | None，无可复制实体；来源由专用 Hook 提供 |
| `SpecialObjectHandler` | ShrineBlood、ShrineRestack | CopyObject；没有实例化物品的能力；处理祭坛组件状态 |

`TrophyHuntersTricorn` 是实际配置字符串，对应 C# 常量 `ObjectType.HuntersTricorn`。三个锅的字符串为 `LunarCauldron_WhiteToGreen`、`LunarCauldron_GreenToRed`、`LunarCauldron_RedToWhite`。

## 不完整或未接入的对象路径

- `LunarShopHandler.cs` 存在但未被 ObjectHandlerManager 注册。虽然 ShopTerminal Hook 识别 `LunarShopTerminal`，默认配置生成不包含它，最终通常回退为 None。
- `TripleShopLarge` 有常量和名称识别，但从 MultiShopHandler 的类型表中注释掉；不要把识别代码当作有效支持。
- `VoidFields`、`PickupLunarCoin` 仅有常量，没有当前注册的处理器。
- 三个 LunarCauldron 类型及 ShrineCleanse 已登记到 PrinterHandler，但 `ShopTerminalBehaviorHandler.Start` 没有识别它们的名称，仓库中未发现对应的其他来源注入。配置可出现，运行时是否进入这条路径仍需补查/实测。
- 无人机、炮塔未实现；第三方交互物需要显式接入。

## Hook 按任务导航

以下文件都位于 `InstancedLoot/Hooks/`。多数名称与游戏组件一致，但不能仅据名称推断处理范围。

| 文件 | 关注点 |
| --- | --- |
| `SpawnCardHandler.cs` | 记录对象实际使用的 SpawnCard |
| `SceneDirectorHandler.cs` | 交互物生成预算；Lockbox、LockboxVoid、FreeChest 的拥有者注入 |
| `PurchaseInteractionHandler.cs` | 提示与服务器实际购买入口的资格检查；交互前记录购买者 Owner |
| `ChestBehaviorHandler.cs`、`OptionChestBehaviorHandler.cs` | Start 中名称识别/复制体接管；掉落上下文 |
| `RouletteChestControllerHandler.cs`、`ShrineChanceBehaviorHandler.cs` | 轮盘箱与机会祭坛的识别和掉落上下文 |
| `MultiShopControllerHandler.cs`、`ShopTerminalBehaviorHandler.cs` | 商店/终端初始化、源类型继承和掉落 |
| `ShrineBloodBehaviorHandler.cs`、`ShrineRestackBehaviorHandler.cs` | 血之祭坛/秩序祭坛的识别与复制体接管 |
| `ScrapperControllerHandler.cs` | 在 ScrappingToIdle 的 IL 中取得交互者并标记废料来源 |
| `PickupDropletControllerHandler.cs` | droplet 创建、CreatePickup（碰撞/强制初始化）、指挥精华转换时传递来源；临时上下文保存并恢复 |
| `GenericPickupControllerHandler.cs`、`PickupGrantPolicy.cs` | pickup 来源保存、实例化、交互/自动拾取与授予前资格检查；按引用更新 GrantContext，让剩余玩家仍可领取 |
| `PickupPickerControllerHandler.cs` | picker 延迟初始化、选择资格、共享选择、输出玩家列表、销毁控制 |
| `SacrificeArtifactManagerHandler.cs` | 牺牲神器掉落归属及概率调整；攻击者优先 attackerOwnerMaster，后取 attackerMaster |
| `BossHunterHandler.cs` | 猎人三角帽使用者及掉落来源 |
| `BossGroupHandler.cs` | Boss 类型识别、掉落数缩放、按掉落循环分配拥有者 |
| `ScavBackpackOpeningHandler.cs` | 拾荒者背包最大掉落数缩减 |
| `DeathRewardsHandler.cs` | 怪物金币奖励按人数缩减 |
| `ChatHandler.cs` | `uninstance`、`uninstanceall` 的命令解析和标记目标查找 |
| `DitherModelHandler.cs` | 原生 DitherModel 与 FadeBehavior 的淡化叠加 |
| `HologramProjectorHandler.cs` | 价格提示的观察者过滤与刷新 |
| `AnimationEventsHandler.cs`、`EffectManagerHandler.cs` | 动态效果与所属对象的 FadeBehavior 关联 |
| `PingHandler.cs` | 标记消息、实例归属文字、提示保留与位置指示器 |
| `DevelopmentHooksHandler.cs` | 仅 DEBUG 注册；修改连接回调、消息 hash、空玩家名 |
| `InteractorHandler.cs` | 源码存在，但 HookManager 的注册语句被注释；当前不是有效交互过滤路径 |

通用 Hook 在 HookManager 构造函数登记；各对象专用 Hook 由 ObjectHandler.Init 登记。判断某个 Hook 是否生效，应追到这两处注册入口，不应只检查 `RegisterHooks` 方法是否存在。
