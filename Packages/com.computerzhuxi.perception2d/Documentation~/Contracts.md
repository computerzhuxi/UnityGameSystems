# 感知契约与边界

## 所有权

运行时程序集 Computerzhuxi.Perception2D 引用 Unity，不引用游戏程序集。Session 唯一拥有记忆和扫描时钟。World 只管理注册及事件路由；调用者通过显式 world 引用区分独立环境。API 仅供 Unity 主线程。

目标身份是组件实例加注册代次，不是存档或网络 ID。目标禁用/解绑结束注册，重新注册产生新代次。旧声音不能关联到新代次。目标 sightDetectable 可独立控制视觉资格。

## 配置与时间

配置在会话初始化时复制并验证，Inspector 运行中修改配置不会重建会话。Configure 只允许在观察者停用且不发布回调时调用，会重置状态。世界位置和朝向必须有限；范围、响度和记忆时长非负；视角 0..360；丢失距离不得小于发现距离；扫描间隔至少 0.02 秒；无来源记忆必须大于零。

使用 Time.timeAsDouble 和缩放 deltaTime。暂停不扫描、不老化、不投递通知。长帧最多扫描一次，不补扫。观察者禁用保留记忆，重新启用后先结束停用前的持续视觉、老化，再使用发现距离采样。扫描新确认丢失的超龄视觉也在本批清理。ResetForReuse 排队重置并取消待处理旧事件；复用流程应先重置并推进更新，再报告新生命周期声音。

## 视觉

未可见使用发现距离，已可见使用丢失距离，均受视角和遮挡限制。Collider 查询使用可增长 List，去重单位为 PerceptionTarget2D。单一检测点默认为 TargetRoot，可配置 sightPoint。目标 Collider 可是 Trigger，障碍 Trigger 不阻挡。自己的子 Collider 和目标自己的 Collider 不作为遮挡。其他障碍起点命中仍算遮挡。

不包含近身穿墙、追踪忽略视角、自动成功范围或抵达位置即遗忘。持续可见更新最后位置，但不重复发送 Acquired。

## 听觉

有效距离=min(HearingRange * Loudness, 可选 MaxRange)。未指定 MaxRange 时没有额外上限。零响度不投递，负数/NaN/Infinity 抛出参数异常。上报时确定位置、时间、来源代次和接收资格，批次中提交刺激。不依赖音频播放，不检查方向，不做墙体衰减。

同一来源每种感官保存最近记录，但每个有效声音均有 Heard 通知。无来源声音以 World 内事件 ID 标识并有限时过期。来源必须注册在同一个 World。

## 记忆与位置

视觉丢失仍保存最后成功位置；只有不可见视觉才按 SightMemory 过期。听觉按 HearingMemory 过期。目标记忆时长零表示不按时间过期。无来源声音按 AnonymousMemory 过期。

综合位置优先当前视觉，否则选择最新有效记录，同时间使用 DominantSense。返回 PerceptionStimulus，包括来源感官和时间；不跟踪不可见对象的实时位置。

## 命令和事件

SetSenseEnabled、ForgetSense、ForgetTarget、ClearMemory、ResetForReuse 在下一批提交。回调内命令也延后。Bind 取消绑定前的旧环境命令，但保留绑定后提交的新命令。ResetForReuse 同时取消本批尚未发布的旧声音通知。听觉关闭请求在非回调调用时立即阻止之后的新声音接收，IsSenseEnabled 查询最近提交状态。

关闭视觉产生 SenseDisabled 且结束当前视觉，保留记忆；关闭听觉不制造声音结束事件。重新开启不补听，视觉安排立即扫描。清除记忆不改变开关；若视觉仍开着，后续扫描可重新发现。

每批先更新状态与 ObservationVersion，再依次通知 SenseUpdated、TargetForgotten、ObservationsUpdated。变化携带 Acquired/Lost/Heard/SenseDisabled/Expired/Cleared/TargetInvalidated。订阅者异常记入 Unity 日志，其他订阅者继续执行。版本只比较是否相等，不依赖严格递增永不回绕。

TargetForgotten 在最终提交状态没有该代任何记录时发布；同批重获同代目标不发布整体遗忘，旧代被新代替代仍发布旧代遗忘。目标注销后的清理在下一次观察者批次提交。查询视图不能跨更新枚举；需要稳定历史时复制值。HeardEvent 的刺激不表示持续发声。

## 调试和限制

选中观察者并启用 showDebugGizmos，青色为视觉，黄色为追踪范围，绿色为听觉和声音，紫色为视觉记忆。Sample 提供感官切换及声音按钮。首版没有 3D、空间索引、网络身份、声学传播、多检测点或任意第三方感官插件 API。
