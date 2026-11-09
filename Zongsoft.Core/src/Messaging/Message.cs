/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging;

/// <summary>表示消息的结构。</summary>
public struct Message
{
	#region 静态字段
	public static readonly Message Empty = new Message();
	#endregion

	#region 成员字段
	private readonly Delegate _acknowledger;
	#endregion

	#region 构造函数
	public Message(string topic, byte[] data) : this(null, topic, data, null, (Delegate)null) { }

	public Message(string topic, byte[] data, Action acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, Action<TimeSpan> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, string tags, Action acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, string tags, Action<TimeSpan> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }

	public Message(string topic, byte[] data, Func<CancellationToken, ValueTask> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(null, topic, data, null, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, string tags, Func<CancellationToken, ValueTask> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }
	public Message(string topic, byte[] data, string tags, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(null, topic, data, tags, (Delegate)acknowledger) { }

	public Message(string identifier, string topic, byte[] data) : this(identifier, topic, data, null, (Delegate)null) { }

	public Message(string identifier, string topic, byte[] data, Action acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, Action<TimeSpan> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, string tags, Action acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, string tags, Action<TimeSpan> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }

	public Message(string identifier, string topic, byte[] data, Func<CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, null, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, string tags, Func<CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }
	public Message(string identifier, string topic, byte[] data, string tags, Func<TimeSpan, CancellationToken, ValueTask> acknowledger) : this(identifier, topic, data, tags, (Delegate)acknowledger) { }

	private Message(string identifier, string topic, byte[] data, string tags, Delegate acknowledger)
	{
		this.Topic = topic;
		this.Data = data;
		this.Tags = tags;
		this.Identifier = identifier;
		this.Identity = null;
		this.Timestamp = DateTime.UtcNow;
		_acknowledger = acknowledger;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置消息主题。</summary>
	public string Topic { get; }

	/// <summary>获取或设置消息内容。</summary>
	public byte[] Data { get; set; }

	/// <summary>获取或设置消息标签。</summary>
	public string Tags { get; set; }

	/// <summary>获取或设置消息的身份标识。</summary>
	public string Identity { get; set; }

	/// <summary>获取或设置消息的标识符。</summary>
	public string Identifier { get; set; }

	/// <summary>获取或设置消息时间戳。</summary>
	public DateTime Timestamp { get; set; }

	/// <summary>获取一个值，指示消息包是否为空包。</summary>
	[Serialization.SerializationMember(Ignored = true)]
	[System.Text.Json.Serialization.JsonIgnore]
	public readonly bool IsEmpty => this.Data == null || this.Data.Length == 0;
	#endregion

	#region 公共方法
	/// <summary>应答消息。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	public readonly ValueTask AcknowledgeAsync(CancellationToken cancellation = default) => this.AcknowledgeAsync(TimeSpan.Zero, cancellation);

	/// <summary>应答消息。</summary>
	/// <param name="delay">指定的应答延迟。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的异步任务。</returns>
	public readonly ValueTask AcknowledgeAsync(TimeSpan delay, CancellationToken cancellation = default)
	{
		switch(_acknowledger)
		{
			case Action acknowledge:
				acknowledge();
				return ValueTask.CompletedTask;
			case Action<TimeSpan> acknowledge:
				acknowledge(delay);
				return ValueTask.CompletedTask;
			case Func<CancellationToken, ValueTask> acknowledge:
				return acknowledge(cancellation);
			case Func<TimeSpan, CancellationToken, ValueTask> acknowledge:
				return acknowledge(delay, cancellation);
			default:
				return ValueTask.CompletedTask;
		}
	}
	#endregion

	#region 重写方法
	public override readonly string ToString()
	{
		if(string.IsNullOrEmpty(this.Topic))
			return "<NoTopic>";

		return string.IsNullOrEmpty(this.Tags) ?
			$"[#{this.Identifier}@{this.Timestamp}] {this.Topic}":
			$"[#{this.Identifier}@{this.Timestamp}] {this.Topic} ({this.Tags})";
	}
	#endregion
}
