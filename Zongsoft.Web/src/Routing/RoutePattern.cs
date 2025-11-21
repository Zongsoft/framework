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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Zongsoft.Web.Routing;

public class RoutePattern : IReadOnlyCollection<RoutePattern.Entry>
{
	#region 常量定义
	const string REGEX_NAME = "name";
	const string REGEX_VALUE = "value";
	const string REGEX_OPTIONAL = "optional";
	const string REGEX_CAPTURES = "captures";
	const string REGEX_ARGUMENTS = "arguments";
	const string REGEX_CONSTRAINT = "constraint";

	const string REGEX_PATTERN = $$"""
	[\[\{]
	(?<{{REGEX_CAPTURES}}>\*{1,2})?
	(?<{{REGEX_NAME}}>\w+)
	(?<{{REGEX_OPTIONAL}}>\?)?
	(
		=
		(?<{{REGEX_VALUE}}>\w+)
	)?
	(:
		(?<{{REGEX_CONSTRAINT}}>\w+
			(?<{{REGEX_ARGUMENTS}}>\([^\{\}\(\)]*\))?
		)
	)*
	[\}\]]
	""";
	#endregion

	#region 静态变量
	private static readonly Regex _regex = new(REGEX_PATTERN, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
	#endregion

	#region 成员字段
	private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 私有构造
	private RoutePattern(string pattern) => this.Value = pattern;
	#endregion

	#region 公共属性
	public int Count => _entries.Count;
	public string Value { get; private set; }
	public Entry this[string name] => name != null && _entries.TryGetValue(name, out var value) ? value : null;
	#endregion

	#region 公共方法
	public int Map(params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(parameters == null)
			return 0;

		int count = 0;

		foreach(var parameter in parameters)
			count += this.Map(parameter.Key, parameter.Value) ? 1 : 0;

		return count;
	}

	public bool Map(string name, string value)
	{
		if(string.IsNullOrEmpty(name))
			return false;

		if(_entries.Remove(name, out var entry))
		{
			var prefix = this.Value.AsSpan()[..entry.Position];
			var suffix = this.Value.AsSpan()[(entry.Position + entry.Length)..];
			var interval = (string.IsNullOrEmpty(value) ? 0 : value.Length) - entry.Length;

			if(string.IsNullOrEmpty(value) && prefix.Length > 0 && suffix.Length > 0 && prefix[^1] == suffix[0])
				this.Value = $"{prefix}{suffix[1..]}";
			else
				this.Value = $"{prefix}{value}{suffix}";

			foreach(var part in _entries.Values)
			{
				if(part.Position > 0)
					part.Position += interval;
			}

			return true;
		}

		return false;
	}
	#endregion

	#region 私有方法
	private bool Add(Match match)
	{
		if(match == null || !match.Success)
			return false;

		var name = match.Groups[REGEX_NAME].Value;
		var value = match.Groups[REGEX_VALUE].Value;
		var optional = match.Groups[REGEX_OPTIONAL].Success;
		var captured = match.Groups[REGEX_CAPTURES].Success;

		return _entries.TryAdd(name, new Entry(match.Index, match.Length, name, value, optional, captured));
	}
	#endregion

	#region 静态方法
	public static RoutePattern Resolve(string pattern)
	{
		if(string.IsNullOrWhiteSpace(pattern))
			return null;

		var result = new RoutePattern(pattern);
		var matches = _regex.Matches(pattern);

		for(int i = 0; i < matches.Count; i++)
			result.Add(matches[i]);

		return result;
	}
	#endregion

	#region 重写方法
	public override string ToString() => this.Value;
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<Entry> GetEnumerator() => _entries.Values.GetEnumerator();
	#endregion

	#region 嵌套子类
	public sealed class Entry : IEquatable<Entry>
	{
		internal int Position;
		internal readonly int Length;

		internal Entry(int position, int length, string name, string value, bool optional = false, bool captured = false)
		{
			this.Position = position;
			this.Length = length;
			this.Name = name;
			this.Value = value;
			this.Optional = optional;
			this.Captured = captured;
			this.Constraints = [];
		}

		public string Name { get; }
		public string Value { get; }
		public bool Optional { get; }
		public bool Captured { get; }
		public bool HasValue => !string.IsNullOrWhiteSpace(this.Value);
		public ConstraintCollection Constraints { get; internal set; }

		public bool Equals(Entry other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => this.Equals(obj as Entry);
		public override int GetHashCode() => this.Name.GetHashCode();
		public override string ToString()
		{
			if(this.Optional)
				return string.IsNullOrEmpty(this.Value) ? $"{this.Name}?" : $"{this.Name}?={this.Value}";
			if(this.Captured)
				return string.IsNullOrEmpty(this.Value) ? $"*{this.Name}" : $"*{this.Name}={this.Value}";

			return string.IsNullOrEmpty(this.Value) ? this.Name : $"{this.Name}={this.Value}";
		}
	}

	public sealed class Constraint(string name, params string[] arguments)
	{
		public string Name { get; } = name;
		public string[] Arguments { get; } = arguments;
		public bool HasArguments => this.Arguments != null && this.Arguments.Length > 0;
		public override string ToString() => this.HasArguments ? $"{this.Name}({string.Join(',', this.Arguments)})" : this.Name;
	}

	public sealed class ConstraintCollection : KeyedCollection<string, Constraint>
	{
		protected override string GetKeyForItem(Constraint constraint) => constraint.Name;
	}
	#endregion
}
