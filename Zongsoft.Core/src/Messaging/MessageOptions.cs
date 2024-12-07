using System;

namespace Zongsoft.Messaging;

/// <summary>
/// 表示消息主题订阅操作的选项类。
/// </summary>
public class MessageSubscribeOptions
{
	#region 单例字段
	public static readonly MessageSubscribeOptions Default = new MessageSubscribeOptions();
	#endregion

	#region 构造函数
	public MessageSubscribeOptions(MessageReliability reliability = MessageReliability.MostOnce, MessageFallbackBehavior fallbackBehavior = MessageFallbackBehavior.Backoff)
	{
		this.Reliability = reliability;
		this.FallbackBehavior = fallbackBehavior;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置订阅消息回调的可靠性。</summary>
	public MessageReliability Reliability { get; set; }

	/// <summary>获取或设置订阅回调失败的重试策略。</summary>
	public MessageFallbackBehavior FallbackBehavior { get; set; }
	#endregion
}

/// <summary>
/// 表示消息出队(消费)操作的选项类。
/// </summary>
public class MessageDequeueOptions
{
	#region 单例字段
	public static readonly MessageDequeueOptions Default = new MessageDequeueOptions(TimeSpan.FromSeconds(10));
	#endregion

	#region 构造函数
	public MessageDequeueOptions() : this(TimeSpan.Zero) { }
	public MessageDequeueOptions(TimeSpan timeout)
	{
		this.Timeout = timeout;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置出队超时。</summary>
	public TimeSpan Timeout { get; set; }
	#endregion
}

/// <summary>
/// 表示消息入队(生产)操作的选项类。
/// </summary>
public class MessageEnqueueOptions
{
	#region 单例字段
	public static readonly MessageEnqueueOptions Default = new();
	#endregion

	#region 构造函数
	public MessageEnqueueOptions(byte priority = 0) : this(MessageReliability.MostOnce, priority) { }
	public MessageEnqueueOptions(MessageReliability reliability, byte priority = 0) : this(TimeSpan.Zero, reliability, priority) { }
	public MessageEnqueueOptions(TimeSpan delay, MessageReliability reliability = MessageReliability.MostOnce, byte priority = 0)
	{
		this.Delay = delay;
		this.Expiry = TimeSpan.Zero;
		this.Priority = priority;
		this.Reliability = reliability;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置入队的延迟时长。</summary>
	public TimeSpan Delay { get; set; }

	/// <summary>获取或设置消息的有效期。</summary>
	public TimeSpan Expiry { get; set; }

	/// <summary>获取或设置消息的优先级。</summary>
	public byte Priority { get; set; }

	/// <summary>获取或设置消息的可靠性。</summary>
	public MessageReliability Reliability { get; set; }
	#endregion
}
