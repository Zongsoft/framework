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

namespace Zongsoft.Data;

/// <summary>表示数据模式的接口。</summary>
/// <typeparam name="TMember">泛型参数，表示数据模式的成员类型。</typeparam>
public interface ISchema<TMember> : ISchema where TMember : SchemaMemberBase
{
	/// <summary>获取数据模式元素集合。</summary>
	SchemaMemberCollection<TMember> Members { get; }

	/// <summary>查找指定路径的模式元素。</summary>
	/// <param name="path">指定要查找的元素路径，路径是以句点(<c>.</c>)或斜杠(<c>/</c>)为分隔符而连接的成员名字符串。</param>
	/// <returns>返回找到的模式元素，如果查找失败则返回空(<c>null</c>)。</returns>
	new TMember Find(string path);

	/// <summary>添加一个元素到位于指定路径处的元素集中。</summary>
	/// <param name="path">指定要添加的元素路径，路径是以句点(<c>.</c>)或斜杠(<c>/</c>)为分隔符而连接的成员名字符串。</param>
	/// <returns>返回当前数据模式。</returns>
	new ISchema<TMember> Include(string path);

	/// <summary>从元素集中移除指定位置的元素。</summary>
	/// <param name="path">指定要移除的元素路径，路径是以句点(<c>.</c>)或斜杠(<c>/</c>)为分隔符而连接的成员名字符串。</param>
	/// <returns>返回当前数据模式。</returns>
	new ISchema<TMember> Exclude(string path);

	/// <summary>从元素集中移除指定位置的元素。</summary>
	/// <param name="path">指定要移除的元素路径，路径是以句点(<c>.</c>)或斜杠(<c>/</c>)为分隔符而连接的成员名字符串。</param>
	/// <param name="member">输出参数，如果排除成功则返回被排除的模式成员。</param>
	/// <returns>返回一个值指示是否排除成功，如果为真(<c>True</c>)则表示成功，否则为失败。</returns>
	bool Exclude(string path, out TMember member);
}
