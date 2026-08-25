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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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
