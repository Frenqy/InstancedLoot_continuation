# 项目文档索引

本套中文文档面向维护者和 AI，依据当前仓库源码及原 README 整理，核对日期为 **2026-10-16**。整理前代码基线为 `46427bb`（`Chore: Add auto packaging script`）。目标是先了解系统，再针对任务阅读少量代码。

当前模组版本为 1.5.4，以 2026-10-08 的 Hallowed Concepts 游戏 1.5.0 API 为编译目标；依赖证据、隔离回归与尚未完成的游戏内验证见 [开发文档](DEVELOPMENT.md)。后续每个修改批次（含仅文档修改）均须按 [版本规则](DEVELOPMENT.md#打包与版本) 递增模组版本。

## 阅读顺序

| 文档 | 用途 |
| --- | --- |
| [AGENTS.md](../AGENTS.md) | AI 首读：项目背景、最小阅读路径、修改约束、常用命令 |
| [ARCHITECTURE.md](ARCHITECTURE.md) | 生命周期、对象/掉落流水线、核心状态、网络与显示 |
| [CODE_MAP.md](CODE_MAP.md) | 功能到文件的导航、处理器注册情况、实际支持边界 |
| [CONFIGURATION.md](CONFIGURATION.md) | 使用方式、模式、预设、配置覆盖规则与聊天命令 |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 环境、构建、打包、新增功能路径、游戏内验证清单 |
| [KNOWN_ISSUES.md](KNOWN_ISSUES.md) | 已知限制、源码疑点、尚未完成或启用的功能 |
| [ISSUE_TRIAGE.md](ISSUE_TRIAGE.md) | 仓库问题核查和待发布的 Issue 草稿；实际发布状态以该文档为准 |
| [原 README](../README.md) | 面向模组使用者的英文说明与项目来源 |
| [CHANGELOG](../CHANGELOG.md) | 历史发布记录；不是当前支持范围的完整清单 |

## 按任务选择入口

| 任务或现象 | 先读 | 再定位 |
| --- | --- | --- |
| 调整哪些东西独立领取 | 配置说明 | `Config.GetInstanceMode`、`DefaultPresets` |
| 新交互物没有实例化 | 代码导航、开发文档 | 对象名称识别、`HandlersForObjectType`、`HandleInstancing` |
| 复制体物品/价格不一致 | 架构 | 对应 ObjectHandler 的 `InstanceSingleObjectFrom` |
| 掉落归属错误或重复领取 | 架构中的掉落流程 | 来源 Hook → droplet → pickup/picker → `InstanceHandler` |
| 主机正常、客户端异常 | 架构中的网络流程、已知问题 | `SyncInstances`、`OnPlayerAdded` |
| 不能交互但仍可见、标记不对 | 架构中的显示流程 | `FadeBehavior`、`PingHandler`、`Utils` |
| 游戏更新后不能加载 | 开发文档、已知问题 | 依赖版本、报错 Hook 的 IL 匹配 |
| 准备发布 | 开发文档 | `manifest.json`、插件版本、项目版本、打包脚本 |

## 文档维护方式

这里只记录仓库实际实现和明确标出的限制，不把注释中的计划当作已完成功能。支持一个对象至少要检查：常量、处理器注册、运行时识别和掉落路径。修改其中一项后应同步更新代码导航和配置说明。

构建验证记录在开发文档；游戏实测结论只有在明确列出环境和场景时才成立。文档不是兼容所有游戏版本或第三方模组的保证。
