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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Components;

public readonly partial struct Identifier
{
	/// <summary>提供标识简写格式的登记入口。</summary>
	/// <remarks>应在交换简写标识前登记双方一致的规则。相同规则可重复登记，冲突规则明确失败；登记和查询支持并发，规则不能替换。</remarks>
	public static class Formatting
	{
		#region 私有字段
		private static readonly ConcurrentDictionary<string, Rule> _names = new(StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<Type, Rule> _types = new();
		#endregion

		#region 登记方法
		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <typeparam name="T">完整键的类型。</typeparam>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T>(string name, char separator = '-') => Register(name, separator, typeof(T));
		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2));
		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3));
		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3, T4>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
		/// <typeparam name="T5">第 5 个键分量的类型。</typeparam>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3, T4, T5>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <typeparam name="T">完整键的类型。</typeparam>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">键类型不支持双向文本转换。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T>(Type type, char separator = '-') => Register(type, separator, typeof(T));
		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">键类型不支持双向文本转换。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2));
		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">键类型不支持双向文本转换。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3));
		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">键类型不支持双向文本转换。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3, T4>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
		/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
		/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
		/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
		/// <typeparam name="T5">第 5 个键分量的类型。</typeparam>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">键类型不支持双向文本转换。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register<T1, T2, T3, T4, T5>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

		/// <summary>为逻辑名称登记键类型及简写分隔符。</summary>
		/// <param name="name">逻辑名称，遵守标识名称规则并忽略大小写。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <param name="types">按键分量顺序排列的一至五个封闭类型，须支持双向文本转换；登记时复制数组。</param>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register(string name, char separator, params Type[] types)
		{
			if(!Identifier.IsName(name))
				throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(name));

			var rule = new Rule(separator, types);
			if(!_names.GetOrAdd(name, rule).Equals(rule))
				throw new InvalidOperationException(Zongsoft.Properties.Resources.Identifier_FormattingConflict_Message);
		}

		/// <summary>为 CLR 类别登记键类型及简写分隔符。</summary>
		/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
		/// <param name="separator">键分量分隔符，仅支持短横线、逗号、斜杠或点。</param>
		/// <param name="types">按键分量顺序排列的一至五个封闭类型，须支持双向文本转换；登记时复制数组。</param>
		/// <exception cref="ArgumentNullException">类别类型为空。</exception>
		/// <exception cref="ArgumentException">名称或键类型不符合登记规则。</exception>
		/// <exception cref="ArgumentOutOfRangeException">不支持指定的分隔符。</exception>
		/// <exception cref="InvalidOperationException">该类别已经登记了不同的键类型或分隔符。</exception>
		/// <remarks>重复登记同一规则不会改变状态。只有实际键类型与规则完全匹配时，默认文本格式才输出简写；完整格式不依赖此规则。</remarks>
		public static void Register(Type type, char separator, params Type[] types)
		{
			ArgumentNullException.ThrowIfNull(type);
			var rule = new Rule(separator, types);
			if(!_types.GetOrAdd(type, rule).Equals(rule))
				throw new InvalidOperationException(Zongsoft.Properties.Resources.Identifier_FormattingConflict_Message);
		}
		#endregion

		#region 查询方法
		internal static Rule Find(string name, Type type) => type != null ? _types.GetValueOrDefault(type) : name == null ? null : _names.GetValueOrDefault(name);
		#endregion

		#region 嵌套子类
		/// <summary>保存分隔符及按顺序排列的简写键类型。</summary>
		internal sealed class Rule : IEquatable<Rule>
		{
			/// <summary>初始化简写规则。</summary>
			/// <param name="separator">简写键分量分隔符。</param>
			/// <param name="types">按顺序排列的键类型；构造时复制数组。</param>
			/// <exception cref="ArgumentOutOfRangeException">分隔符不受支持。</exception>
			/// <exception cref="ArgumentException">类型数量或转换能力不符合规则。</exception>
			public Rule(char separator, Type[] types)
			{
				if(separator is not ('-' or ',' or '/' or '.'))
					throw new ArgumentOutOfRangeException(nameof(separator));

				if(types == null || types.Length is < 1 or > 5 || types.Any(type => type == null || type.ContainsGenericParameters || !Identifier.CanConvert(type)))
					throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(types));

				this.Separator = separator;
				this.Types = (Type[])types.Clone();
			}

			/// <summary>获取简写键分量分隔符。</summary>
			public char Separator { get; }
			/// <summary>获取按键分量顺序排列的类型。</summary>
			/// <remarks>该数组属于规则，调用方不得修改其中的元素。</remarks>
			public Type[] Types { get; }

			public bool Equals(Rule other) => other is not null && this.Separator == other.Separator && this.Types.SequenceEqual(other.Types);
			public override bool Equals(object obj) => obj is Rule other && this.Equals(other);
			public override int GetHashCode()
			{
				var hash = new HashCode();
				hash.Add(this.Separator);

				foreach(var type in this.Types)
					hash.Add(type);

				return hash.ToHashCode();
			}
		}
		#endregion
	}
}
