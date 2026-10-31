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
	/// <summary>提供标识简写格式的登记。</summary>
	public static class Formatting
	{
		#region 私有字段
		private static readonly ConcurrentDictionary<string, Rule> _names = new(StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<Type, Rule> _types = new();
		#endregion

		#region 登记方法
		public static void Register<T>(string name, char separator = '-') => Register(name, separator, typeof(T));
		public static void Register<T1, T2>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2));
		public static void Register<T1, T2, T3>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3));
		public static void Register<T1, T2, T3, T4>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
		public static void Register<T1, T2, T3, T4, T5>(string name, char separator = '-') => Register(name, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
		public static void Register<T>(Type type, char separator = '-') => Register(type, separator, typeof(T));
		public static void Register<T1, T2>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2));
		public static void Register<T1, T2, T3>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3));
		public static void Register<T1, T2, T3, T4>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
		public static void Register<T1, T2, T3, T4, T5>(Type type, char separator = '-') => Register(type, separator, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

		public static void Register(string name, char separator, params Type[] types)
		{
			if(!Identifier.IsName(name))
				throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(name));

			var rule = new Rule(separator, types);
			if(!_names.GetOrAdd(name, rule).Equals(rule))
				throw new InvalidOperationException(Zongsoft.Properties.Resources.Identifier_FormattingConflict_Message);
		}

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
		internal sealed class Rule : IEquatable<Rule>
		{
			public Rule(char separator, Type[] types)
			{
				if(separator is not ('-' or ',' or '/' or '.'))
					throw new ArgumentOutOfRangeException(nameof(separator));

				if(types == null || types.Length is < 1 or > 5 || types.Any(type => type == null || type.ContainsGenericParameters || !Identifier.CanConvert(type)))
					throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(types));

				this.Separator = separator;
				this.Types = (Type[])types.Clone();
			}

			public char Separator { get; }
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
