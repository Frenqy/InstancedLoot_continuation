# 2.0 架构与数据流

当前模组版本 **2.0.0**，目标游戏 1.5.0。2.0 只实现用户策略 `PreferredInstanceMode=InstanceObject`：每位生成时在场的玩家拥有一个独立的原生交互实体。玩家分别支付、开启或修复；普通掉落仍是原版公开掉落，任何玩家都能拾取。没有共享物品领取表、owner-only、预设或放弃领取命令。

## 生命周期

`InstancedLoot.Awake` 先绑定配置，创建 `ObjectInstanceService` 和 HookManager，再注册 `OwnerSnapshot`。OnEnable/OnDisable 成对安装和卸载 Hook、场景与玩家事件。插件 Update 泵送源初始化与客户端快照，并每两秒重发仍存在的实体归属；玩家加入也触发补发。新加入者不会为旧对象新增份额。

```text
原生源 Awake/Start → ObjectLifecycleHooks
  → server-only InitializeSource → 标记根及关联终端 PendingOwnership
  → 等待 adapter.IsReady → CaptureState（不可变状态闭包）
  → SpawnCard.DoSpawn（N-1 份，保留位置/旋转/缩放）
  → 副本原生 Awake（一次）→ 等待副本 Start
  → InitializeCopy 接管 Start → 应用捕获状态 → 固定 Owner
  → 每实体 OwnerSnapshot → 客户端独立解析 → 相机过滤
```

源对象的原生初始化执行一次；源和所有副本各分配一个固定拥有者。副本仍通过 SpawnCard 和网络生成，**不会再次执行随机初始化的原生 Start**。`ObjectAdapter.CaptureState` 捕获最终成本、成本类型、可用状态、独立 RNG 以及各类对象的商品状态，闭包不依赖源对象后续存活。源先被消费或销毁，不会阻止剩余副本初始化。

源等待初始化时，其所有可购买子物都被标记为 pending；服务器拒绝购买，显示层暂时隐藏。创建副本期间失败会销毁已创建的根和独立网络终端，并解除源的 pending，整源回退原版；找不到 SpawnCard 同样回退，不把仅有的原版箱子锁给首人。副本适配失败则销毁失败副本并记录错误，不留下无主可购买物。切换 Run/场景或卸载会清理待初始化根与子物。

## 适配器边界

| 适配器 | 捕获/恢复的状态 |
| --- | --- |
| Chest | Purchase 状态、RNG、dropCount、currentPickup |
| OptionChest | Purchase、RNG、独立的 generatedPickups 列表 |
| Roulette | Purchase、轮盘 RNG |
| MultiShop | 控制器 cost/available/RNG；每个终端的 Purchase、pickup、hidden、RNG、hasStarted |
| Shrine | Purchase、Chance/Order RNG；Blood/Chance/Order 指向各自 PurchaseInteraction |
| Repair | Purchase、masterPrefab、NetworkdroneUpgradeCount、destroyAfterSummoning |

三选一原生 **Awake 已调用 CreateTerminals**。新适配器复用这些终端，绝不再建第二组。源控制器等所有终端 `hasStarted` 后才捕获，包含 selfGeneratePickup 商品；副本终端即使先于控制器 Start 也会被接管，避免重抽商品。每玩家一套 controller 与 terminal，购买后的关闭、Executive Card、冷却和失败结果由原生事件决定。根和对应终端槽位分别有稳定 groupId，groupId 只供显示关联，**不会把消费状态共享到其他玩家**。

修理支持位于带 PurchaseInteraction、masterPrefab、destroyAfterSummoning 的 Broken Drone/Turret 物体。原生 onPurchase 继续调用 SummonMasterBehavior，原 activator 传入 MasterSummon，所以新随从归购买者。活动 CharacterBody 不挂实例组件。升级数通过 SyncVar setter 复制，之后只补跑一次不生成随从的原生显示 Start。

## 权限与购买

`InstanceHandler.Owner` 是唯一交互归属，不因 native available 的改变而移除。`InstanceAccessPolicy` 从 Interactor → CharacterBody.master → PlayerCharacterMasterController 解析玩家。提示检查、服务器实际购买入口及服务器 PerformInteraction 均检查固定归属；视觉隐藏不能代替权限判断。

受管 Purchase 使用根/商店控制器事务锁防重入，再调用原生方法；不自行扣钱、开箱、关闭终端或把冷却视为永久消费。未受管对象的 Purchase 入口直接透传原生，包括打印机、锅与净化池。金钱扣除与特殊道具优惠仍由原生 CostTypeDef.PayCost 处理，默认不团队扣钱。

`InteractorCandidateIL` 替换 FindBestInteractableObject 的 raycast 与 overlap 两个 TryGetComponent 入口。受管对象聚合有效 IInteractable，跳过禁用的 picker 并优先可用组件；未受管对象仍返回原生 TryGetComponent 的结果，不改变打印机候选行为。服务器 PerformInteraction 的多组件循环保持原样，只在其之前做整体归属校验。

## 网络协议

`OwnerSnapshot` 是替代旧 SyncInstances 的新消息，两端必须使用相同 2.0 版本。顺序为：

```text
protocolVersion(int=2), runId(NetworkInstanceId), stageClearCount(int), scene(string), sceneEpoch(int),
targetId(NetworkInstanceId), ownerId(NetworkInstanceId), revision(int), groupId(int)
```

一条消息只描述一个实体，不等源对象或同组其他实体。快照可早于 Run、stage SyncVar、场景、target 或 owner 到达；接收后排队，匹配 session 才应用。高 epoch 淘汰同 Run 的旧 epoch；场景切换提高已接受 session 的 epoch 下限，旧包不能给复用的 netId 恢复归属。同实体低 revision 被拒绝；相同 revision 的周期重发允许重试。每次解析重新查询 target 与 owner，不缓存被销毁对象。

pending 最多等待 30 秒；超时或显式 Clear 会解除它创建的临时 PendingOwnership，并恢复默认显示，后续周期重发仍能恢复归属。旧场景包清理，提前收到的新场景包保留。未知元数据的普通原生对象保持原版显示/提示；已收到快照但 owner 尚未解析的对象暂时隐藏并禁止交互。服务器始终作最终权限判断，客户端短暂缺元数据不能越权购买。协议没有同步配置，原生商品和成本仍由游戏 SyncVar 传输。

## 显示

FadeBehavior 按每个 SceneCamera 的玩家分别计算，使用 Renderer.forceRenderingOff 保留原生 renderer.enabled；Light/Highlight 临时过滤和材质属性在 PostRender 恢复。根与终端只管理最近 FadeBehavior 所属的 renderer/effect，防止嵌套快照互相覆盖。无玩家的观察相机显示原生实体；分屏相机各自恢复与重算。

DitherModel、全息提示和动画非池化效果仍保留适配；Ping marker 按当前相机查对应玩家副本，原生 ping 目标和高亮仍有待游戏验证。显示不改世界碰撞层，隐藏副本仍可能参与物理查询；交互候选有独立过滤。
