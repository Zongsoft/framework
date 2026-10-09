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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Zongsoft.Expressions;

/// <summary>表示基于内存字典的变量提供程序。</summary>
/// <remarks>
/// 	<para>使用 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较键，支持字典的索引器、集合初始化器、增删及枚举操作。</para>
/// 	<para>
/// 		默认命名空间的变量直接以名称作为键，例如 <c>name</c>；具名命名空间的变量以 <c>命名空间:名称</c> 作为键，例如 <c>app:price</c>。
/// 		冒号是命名空间限定分隔符，命名空间和变量名称本身不应包含该字符。字典操作接受完整的键，不解析模板语法。
/// 	</para>
/// 	<para>变量值可以为 <see langword="null"/>；查询返回原始对象，不进行类型转换、成员访问、模板求值或对象复制。</para>
/// 	<para>每次查询均读取当前字典。此类不提供额外的并发同步，多线程读写时由调用方负责同步。</para>
/// </remarks>
public class Variables : Dictionary<string, object>, IVariables
{
	#region 静态字段
	private static readonly ConditionalWeakTable<IDictionary<string, object>, DictionaryVariables> _cache = new();
	#endregion

	#region 构造函数
	/// <summary>初始化一个空的变量字典。</summary>
	public Variables() : base(StringComparer.OrdinalIgnoreCase) { }

	/// <summary>使用指定的初始容量初始化变量字典。</summary>
	/// <param name="capacity">字典可容纳的初始变量数量，必须大于或等于零。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于零。</exception>
	public Variables(int capacity) : base(capacity, StringComparer.OrdinalIgnoreCase) { }

	/// <summary>复制指定集合中的键和值以初始化变量字典。</summary>
	/// <param name="variables">初始变量集合，键可以是默认命名空间的名称，也可以是 <c>命名空间:名称</c> 形式的完整键。</param>
	/// <exception cref="ArgumentNullException"><paramref name="variables"/> 为 <see langword="null"/>，或者集合中存在空引用的键。</exception>
	/// <exception cref="ArgumentException">集合中存在按忽略大小写规则比较后重复的键。</exception>
	/// <remarks>
	/// 	<para>字典独立保存集合中的条目，后续增删不影响源集合；变量值仍引用原始对象，不进行深复制。</para>
	/// 	<para>无论源集合采用何种比较方式，新字典均使用 <see cref="StringComparer.OrdinalIgnoreCase"/>。</para>
	/// </remarks>
	public Variables(IEnumerable<KeyValuePair<string, object>> variables) : base(variables, StringComparer.OrdinalIgnoreCase) { }
	#endregion

	#region 静态方法
	/// <summary>将指定字典包装为共享其当前内容的变量视图。</summary>
	/// <param name="dictionary">作为变量来源的字典，不能为 <see langword="null"/>。</param>
	/// <returns>字典本身实现的 <see cref="IVariables"/>，或按字典实例复用的只读变量视图。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="dictionary"/> 为 <see langword="null"/>。</exception>
	/// <remarks>
	/// 	<para>视图读取原字典的当前内容，不复制条目或缓存变量值。默认变量以名称作为键，具名变量以 <c>命名空间:名称</c> 作为键；查询参数不裁剪或拆分。</para>
	/// 	<para>查询按 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较键；枚举查询时返回第一个匹配条目的原始值，包括 <see langword="null"/>。</para>
	/// 	<para>已知采用上述比较器的标准字典直接查询，其它字典枚举匹配。名称为 <see langword="null"/> 时查询抛出 <see cref="ArgumentNullException"/>。</para>
	/// 	<para>缓存按引用身份复用视图，不阻止无外部引用的字典和视图被回收。缓存支持并发访问，源字典的并发读写仍由调用方负责。</para>
	/// </remarks>
	public static IVariables Wrap(IDictionary<string, object> dictionary)
	{
		ArgumentNullException.ThrowIfNull(dictionary);
		return dictionary as IVariables ?? _cache.GetValue(dictionary, static source => new DictionaryVariables(source));
	}
	#endregion

	#region 公共方法
	/// <summary>尝试获取指定命名空间中指定名称的变量值。</summary>
	/// <param name="namespace">要查询的命名空间，<see langword="null"/> 或空字符串均表示忽略命名空间限定，查询默认命名空间；命名空间比较忽略大小写。</param>
	/// <param name="name">不含命名空间限定部分的变量名称，不能为 <see langword="null"/>；名称比较忽略大小写。</param>
	/// <param name="value">查询成功时返回原始变量值，该值可以为 <see langword="null"/>；查询失败时返回 <see langword="null"/>。</param>
	/// <returns>找到变量则返回 <see langword="true"/>，包括值为 <see langword="null"/> 的变量；未找到则返回 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	/// <remarks>
	/// 	<para>命名空间为 <see langword="null"/> 或空字符串时直接查询 <paramref name="name"/> 对应的键；非空命名空间查询 <c>命名空间:名称</c> 对应的键，不回退到默认命名空间。</para>
	/// 	<para>参数不做裁剪或拆分；仅含空白字符的命名空间属于非空命名空间，不会被忽略。</para>
	/// </remarks>
	public bool TryGetValue(string @namespace, string name, out object value)
	{
		ArgumentNullException.ThrowIfNull(name);
		return base.TryGetValue(string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}:{name}", out value);
	}
	#endregion

	#region 嵌套子类
	private sealed class DictionaryVariables(IDictionary<string, object> dictionary) : IVariables
	{
		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string @namespace, string name, out object value)
		{
			ArgumentNullException.ThrowIfNull(name);
			var key = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}:{name}";

			switch(dictionary)
			{
				case Dictionary<string, object> source when source.GetType() == typeof(Dictionary<string, object>) && ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase):
					return source.TryGetValue(key, out value);
				case ConcurrentDictionary<string, object> source when source.GetType() == typeof(ConcurrentDictionary<string, object>) && ReferenceEquals(source.Comparer, StringComparer.OrdinalIgnoreCase):
					return source.TryGetValue(key, out value);
			}

			foreach(var entry in dictionary)
			{
				if(!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
					continue;

				value = entry.Value;
				return true;
			}

			value = null;
			return false;
		}
	}
	#endregion
}
