# 2.0 配置

配置文件仍是 `BepInEx/config/com.kuberoot.instancedloot.cfg`，GUID 没有改变。主机与客户端需要同版本；对象是否实例化以服务器配置为准，归属通过网络发送。客户端配置文件不会自动同步。

## 唯一分配策略

`[General] PreferredInstanceMode = InstanceObject` 是唯一支持值。生成时每位在场玩家分得一个独立原生购买实体，各自支付和使用；对应商品、初始价格和 RNG 相同，之后价格、冷却及消费互不影响。掉落仍是公开物品，任何人都能捡。

2.0 删除 InstanceItems、InstanceBoth、owner-only、Default/InstancePreferred 模式，以及 Preset、ObjectAliases、SharePickupPickers、uninstance/uninstanceall。旧配置项不再读取；升级前可备份旧 cfg，再让 2.0 生成新配置，按本页重新选择开关。没有旧模式到新布尔值的自动迁移。

## 逐项开关

所有开关默认 true，只影响后续生成的对象；局中修改不会重建已分配对象。

| 配置节 | key | 作用 |
| --- | --- | --- |
| Shrines | ShrineChance | 机会神龛，每人自己的价格、失败/成功序列及原生刷新 |
| Shrines | ShrineBlood | 血之神龛，每人独立使用次数；金币仍由原版给购买者 |
| Shrines | ShrineRestack | 秩序神龛，仅重排购买者背包；每人独立原生物体 |
| Repairs | Drone | 损坏无人机待修复物，修理后原生召唤购买者的随从 |
| Repairs | Turret | 损坏炮塔待修复物 |
| Objects | 各支持对象名称 | 箱、选择箱、轮盘、三选一等单独开关，完整列表见代码导航 |

false 让该类交互物保留原版共享行为。未知神龛和全局效果神龛不接入；打印机、锅、净化池不受上述开关影响。

## 平衡配置

保留旧键所在的 `[General - Balance]`，已有同名设置可继续读取：

| key | 默认 | 源码行为 |
| --- | --- | --- |
| ReduceInteractibleBudget | true | SceneDirector 的人数预算按单人计算；所有开关都关闭时如需完全原版生成数量，应将本项也设 false |
| ReduceMoneyDrops | false | 怪物原生团队金币奖励除以参与人数，分母至少 1；保留整数舍入 |

复制时不降低单个物体价格，不修改支付规则，也不让箱子扣所有玩家的钱。实际金币支付由原生 PayCost 的 activatorMaster 完成，Executive Card 回调保留。原版 Longstanding Solitude、免费购买等特殊支付机制继续走游戏自身逻辑；具体组合仍待联机验证。旧 Sacrifice/Boss/Scav 掉落缩减配置已移除，因为相应物品实例化路径不再存在。
