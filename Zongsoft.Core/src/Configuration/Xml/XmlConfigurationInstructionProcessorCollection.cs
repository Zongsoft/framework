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

namespace Zongsoft.Configuration.Xml;

/// <summary>表示线程安全、按名称忽略大小写的 XML 处理指令注册表。</summary>
/// <remarks>注册后保留原实例，不允许隐式替换；枚举使用快照，执行指令时不持有注册表锁。</remarks>
public sealed class XmlConfigurationInstructionProcessorCollection : IReadOnlyCollection<XmlConfigurationInstructionProcessor>
{
	#region 成员字段
	private readonly Collections.SynchronizedDictionary<string, XmlConfigurationInstructionProcessor> _processors = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	/// <summary>获取已注册的指令处理器数量。</summary>
	public int Count => _processors.Count;

	/// <summary>获取指定名称的指令处理器。</summary>
	public XmlConfigurationInstructionProcessor this[string name] => _processors[name];
	#endregion

	#region 公共方法
	/// <summary>注册指令处理器；空实例或重名注册将抛出异常。</summary>
	/// <param name="processor">待注册的处理器实例。</param>
	public void Add(XmlConfigurationInstructionProcessor processor)
	{
		ArgumentNullException.ThrowIfNull(processor);
		_processors.Add(processor.Name, processor);
	}

	/// <summary>判断是否已经注册指定名称的指令。</summary>
	/// <returns>如果已注册则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <param name="name">指令名称。</param>
	public bool Contains(string name) => _processors.ContainsKey(name);

	/// <summary>尝试获取指定名称的指令处理器。</summary>
	/// <returns>如果已注册则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <param name="name">指令名称。</param>
	/// <param name="processor">找到的处理器实例。</param>
	public bool TryGetValue(string name, out XmlConfigurationInstructionProcessor processor) => _processors.TryGetValue(name, out processor);
	#endregion

	#region 内部方法
	internal Dictionary<string, XmlConfigurationInstructionProcessor> GetSnapshot() => new(_processors, StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<XmlConfigurationInstructionProcessor> GetEnumerator() => _processors.Values.GetEnumerator();
	#endregion
}
