# 2.0 已知边界与待验证项目

2.0.0 的源码、构建、隔离回归和真实 DLL 静态检查已经覆盖新核心；**尚未在 Unity/Mono 安装这批 Hook，也未做主机/客户端或分屏游戏实测**。不得把 stub/IL 通过写成多人效果已验证。

- 只支持 InstanceObject。普通掉落、Command、Boss、牺牲掉落和 picker 都是原版；没有防止其他玩家捡走某人开箱奖励的机制。
- 迟加入仅获得现存对象归属，不给已经生成的交互物增加新份额；断线玩家的副本不会自动转让。
- 未收到元数据时客户端保留原版显示/提示，可能暂时看到重叠实体。已知快照但 owner 未解析时暂时隐藏；30 秒超时释放临时标记，周期重发负责后续恢复。服务器归属校验始终有效。
- SpawnCard 缺失或副本生成失败会记录错误并整源回退原版。损坏的 DT*BrokenBody 等特殊修理物可能没有对应 card，不能保证独立修复；活动 drone/turret body 不作对象实例化。
- 副本初始化失败会销毁失败副本并记录错误；其拥有者失去该份，模组不会额外创建公开补偿掉落。
- 三选一保留原生关闭与 Executive Card；selfGeneratePickup、配送终端及合法特殊购买机制仍需要游戏内验证。未知/全局神龛、DLC 新物体、第三方交互物无专用支持。
- Shader/dither、非池化动画效果、价格全息和 ping marker 有接入；池化效果归属、原生 ping target/高亮重定向、不同材质与观察相机显示仍有游戏验证边界。隐藏模型没有移除物理碰撞。
- Config 不通过自定义消息同步；两端需要相同 2.0 二进制，旧 SyncInstances 协议不兼容。局中开关不会重建旧对象。
- 预算/金币缩减是显式平衡选项，不是经过游戏平衡实测的保证。

旧模式、旧 IL 失效历史与 1.x 验证记录见 [HISTORY_1_X.md](HISTORY_1_X.md)。旧共享 pickup、掉落来源及组同步路径的问题随这些路径移除，不再是 2.0 当前实现的缺陷。仍保留的 AnimationEventsHandler.CurrentAnimationEvents 尚未用 try/finally 恢复旧值，异常或嵌套调用可能污染显示上下文；这是存活的历史风险，尚待专项修复。历史测试夹具在 validation/PickupGrantILRegression/Historical，仅用于保留负例和历史回归，不进入模组 DLL。
