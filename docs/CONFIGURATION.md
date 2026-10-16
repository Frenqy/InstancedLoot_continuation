# 使用与配置说明

## 安装与配置位置

主机和每位客户端都需要安装模组及 `manifest.json` 中的运行依赖。手动安装时，将发布包内的 `InstancedLoot` 文件夹放入 `Risk of Rain 2/BepInEx/plugins/`。构建和发布包结构见 [开发文档](DEVELOPMENT.md)。

`Debug/LogInstanceInteractions` 默认关闭。启用后，服务器购买路径和客户端交互提示拒绝会记录对象 netId、source/clone、OrigPlayer、Players、available、hidden、pickup 与购买者，适合复现归属问题；多人长时间运行时建议保持关闭。

首次由 BepInEx 加载并初始化后，配置文件为：

```text
BepInEx/config/com.kuberoot.instancedloot.cfg
```

默认 `Preset=Default`、`PreferredInstanceMode=InstanceObject`。对于普通箱子，这表示每人可各开一次，但开出的物品可被其他人拾取。协议没有配置文件同步功能；多人验证时应明确记录各端配置，主要分配决策在服务器路径执行。

## 实例化模式

下表描述模式语义；具体来源的可选模式取决于处理器能力及限制，最终值可能降级。

| 模式 | 交互物 | 掉落物 |
| --- | --- | --- |
| `None` | 原生行为 | 不由该来源实例化 |
| `Default` | 不覆盖上一级 | 全链没有有效设置时为 None |
| `InstancePreferred` | 使用 PreferredInstanceMode | 同左，再按来源能力降级 |
| `InstanceObject` | 通常为每位玩家复制一个对象 | 谁都可拿该对象产生的掉落 |
| `InstanceItems` | 共用一个对象 | 各玩家可独立领取一份 |
| `InstanceBoth` | 通常为每位玩家复制一个对象 | 每个对象的掉落仅供其购买/获得者 |
| `InstanceItemForOwnerOnly` | 共用一个对象 | 仅购买/获得者可领取 |
| `InstanceObjectForOwnerOnly` | 只允许固有拥有者交互 | 掉落公开 |
| `InstanceBothForOwnerOnly` | 只允许固有拥有者交互 | 掉落也仅供拥有者 |

固有拥有者对象目前为 `Lockbox`、`LockboxVoid`、`FreeChestMultiShop`。owner-only 路径缺少 Owner 和 PlayerOverride 时不会强行实例化。普通装备拾取不参与物品实例化，因为它涉及交换逻辑；血之祭坛/秩序祭坛没有物品模式。

内部 `ObjectInstanceMode.None` 仅表示不能复制该来源对象，不等于禁止其掉落独立领取。打印机、废料机、Boss 掉落等正属于这种情况。

## 配置如何合并

实际算法在 `Config.GetInstanceMode`。先以 None 为初值，再依次合并下列层级；非 Default 值覆盖之前的结果：

1. 预设中的各个 alias。
2. 预设中的具体 ObjectType。
3. 用户 `[ObjectAliases]` 的各个 alias。
4. 用户 `[ObjectTypes]` 的具体类型。

多个 alias 按 `SortedSet<string>` 的排序遍历，后处理的非 Default 值覆盖前者；不是“越具体的 alias 优先”。若同一对象属于多个组且设置冲突，用 ObjectTypes 明确覆盖最稳妥。

每层在合并前先替换 `InstancePreferred`，再按对象可选模式降级。例如：

- `InstanceBoth` 优先尝试 `InstanceObject`，然后 `InstanceItems`。
- `InstanceObject` 可退到 `InstanceItems`；`InstanceItems` 可退到 `InstanceObject`。
- `InstanceBothForOwnerOnly` 优先尝试 `InstanceObjectForOwnerOnly`，再 `InstanceItemForOwnerOnly`，并继续展开可用回退。
- 无有效降级路径时返回 None；一个显式 None 会覆盖之前结果。

对象可选模式取决于注册的 ObjectHandler、`CanObjectBeOwned` 和 Enums 中的禁用属性；alias 的可选模式基于其成员模式集合的交集。未登记的对象类型即使有常量，也不会自动生成配置。

结果按 objectType 缓存；InstanceMode 类型配置或预设改变时清空缓存。现有场景对象不会因此自动撤销并重建实例，建议在下一局验证配置；历史 README 特别提示中途修改可能让当前关卡掉落过多。

## 内置预设

来源：`Configuration/DefaultPresets.cs`。

| 预设 | 当前定义 |
| --- | --- |
| `None` | 预设本身不启用实例化；用户类型/alias 覆盖仍可生效 |
| `Default` | Chests、Shops、Shrines、LunarChest、VoidTriple 跟随首选模式；Equipment 固定 InstanceObject；ItemSpawned 为 InstanceObjectForOwnerOnly；Sacrifice 与 TrophyHuntersTricorn 固定 InstanceItems |
| `Selfish` | 大量来源设为 InstanceItemForOwnerOnly；ItemSpawned 为 InstanceBothForOwnerOnly；Equipment 仍为 InstanceObject，因此不能把它理解为所有来源都绝不增加数量 |
| `EVERYTHING` | 对查询到的所有名称返回 InstancePreferred，再按能力降级；不会激活未注册对象，源码描述不推荐此预设 |

Default 并未显式启用所有来源，例如普通 Boss 奖励没有该预设中的专门配置。最终结果还受 alias、模式能力和用户覆盖影响。

## 通用和平衡选项

| 配置节 / 键 | 默认值 | 实际用途 |
| --- | --- | --- |
| General / Preset | `Default` | 选择预设 |
| General / PreferredInstanceMode | `InstanceObject` | 替换 InstancePreferred |
| General / SharePickupPickers | `false` | false 为每人分别选择；true 为一人选择后产出供剩余玩家独立领取的相同物品 |
| General - Balance / ReduceInteractibleBudget | `true` | SceneDirector 预算人数按 1 处理；不考虑实例化模式，源码标注未测试 |
| General - Balance / ReduceSacrificeSpawnChance | `true` | 若牺牲模式增加物品数，掉率除以参与人数；源码标注未测试 |
| General - Balance / ReduceBossDrops | `true` | 若该 Boss 模式增加物品数，临时关闭奖励按人数缩放 |
| General - Balance / ReduceScavengerSackDrops | `false` | 若背包模式增加物品数，最大掉落数按人数做整数除法 |
| General - Balance / ReduceMoneyDrops | `false` | 怪物金币奖励按参与人数做整数除法；源码标注未测试 |

`Utils.IncreasesItemCount` 只对 InstanceObject、InstanceItems、InstanceBoth 返回 true。这些开关没有构成完整的游戏经济平衡模型；特别是 Preset=None 并不会自动关闭独立的预算开关。

## 配置片段

修改游戏已生成的配置文件中的对应项即可，不需要另建文件。

普通箱子只开一次，每位玩家独立领取（其余组按 Default 预设）：

```ini
[General]
Preset = Default
PreferredInstanceMode = InstanceObject

[ObjectAliases]
Chests = InstanceItems
```

仅对普通小箱改为购买者专属掉落，具体类型覆盖上面的 Chests 设置：

```ini
[ObjectTypes]
Chest1 = InstanceItemForOwnerOnly
```

如果需要接近原生行为，可从 Preset=None 开始，将已有 ObjectTypes/ObjectAliases 覆盖恢复为 Default，并关闭独立平衡选项；仅切换预设不会清除用户覆盖。

## 聊天命令

- 标记自己有资格领取的物品，在聊天中输入 `uninstance`，放弃该份领取资格并尝试生成公开掉落。
- 输入 `uninstanceall`，遍历当前符合检查条件的实例，尝试放弃自己的领取资格。

当前实现仅在目标具有 `CreatePickupInfoTracker` 时真正重建 droplet 并移除玩家资格。虽然原 README 也描述选择器可用，代码不能保证所有 picker 或交互物都能放弃；应实测具体生成路径。这不是永久关闭该来源实例化的配置命令。
