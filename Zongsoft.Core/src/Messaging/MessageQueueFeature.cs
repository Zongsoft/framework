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
using System.Collections.ObjectModel;

namespace Zongsoft.Messaging;

/// <summary>表示消息队列支持的一项功能。</summary>
public class MessageQueueFeature : IEquatable<MessageQueueFeature>
{
	#region 公共字段
	/// <summary>表示消息延迟入队功能。</summary>
	public static readonly MessageQueueFeature Delay = new(nameof(Delay));
	/// <summary>表示消息负载压缩功能。</summary>
	public static readonly MessageQueueFeature Compression = new(nameof(Compression));
	#endregion

	#region 成员字段
	private readonly string _name;
	#endregion

	#region 构造函数
	/// <summary>初始化消息队列功能。</summary>
	/// <param name="name">指定功能名称。</param>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为空或空白。</exception>
	public MessageQueueFeature(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim().ToLowerInvariant();
	}
	#endregion

	#region 公共属性
	/// <summary>获取功能名称。</summary>
	public string Name => _name;
	#endregion

	#region 重写方法
	public bool Equals(MessageQueueFeature other) => other is not null && string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is MessageQueueFeature other && this.Equals(other);
	public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
	public override string ToString() => _name;
	#endregion
}

/// <summary>表示消息队列功能集合。</summary>
public class MessageQueueFeatureCollection() : KeyedCollection<string, MessageQueueFeature>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(MessageQueueFeature feature) => feature.Name;
}
