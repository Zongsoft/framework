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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

/// <summary>表示线程安全、按名称忽略大小写的 Profile 指令注册表。</summary>
/// <remarks>注册后保留原实例，不允许隐式替换；枚举使用快照，执行指令时不持有注册表锁。</remarks>
public sealed class ProfileDirectiveCollection : IReadOnlyCollection<ProfileDirectiveBase>
{
	#region 成员字段
	private readonly object _sync = new();
	private readonly Dictionary<string, ProfileDirectiveBase> _directives = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	/// <summary>获取已注册的指令数量。</summary>
	public int Count
	{
		get
		{
			lock(_sync)
				return _directives.Count;
		}
	}

	/// <summary>获取指定名称的指令。</summary>
	public ProfileDirectiveBase this[string name]
	{
		get
		{
			lock(_sync)
				return _directives[name];
		}
	}
	#endregion

	#region 公共方法
	/// <summary>注册指令；空实例或重名注册将抛出异常。</summary>
	/// <param name="directive">待注册的指令实例。</param>
	public void Add(ProfileDirectiveBase directive)
	{
		ArgumentNullException.ThrowIfNull(directive);

		lock(_sync)
			_directives.Add(directive.Name, directive);
	}

	/// <summary>判断是否已经注册指定名称的指令。</summary>
	/// <returns>如果已注册则返回真，否则返回假。</returns>
	/// <param name="name">指令名称。</param>
	public bool Contains(string name)
	{
		lock(_sync)
			return _directives.ContainsKey(name);
	}

	/// <summary>尝试获取指定名称的指令。</summary>
	/// <returns>如果已注册则返回真，否则返回假。</returns>
	/// <param name="name">指令名称。</param>
	/// <param name="directive">找到的指令实例。</param>
	public bool TryGetValue(string name, out ProfileDirectiveBase directive)
	{
		lock(_sync)
			return _directives.TryGetValue(name, out directive);
	}
	#endregion

	#region 内部方法
	internal Dictionary<string, ProfileDirectiveBase> Snapshot()
	{
		lock(_sync)
			return new(_directives, StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 枚举遍历
	public IEnumerator<ProfileDirectiveBase> GetEnumerator() => this.Snapshot().Values.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion
}
