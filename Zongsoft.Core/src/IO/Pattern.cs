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
using System.Text;
using System.Text.RegularExpressions;

namespace Zongsoft.IO;

/// <summary>表示文件系统搜索模式的结构。</summary>
/// <remarks>
/// 	<para>搜索模式为空(<c>null</c>)或空字符串(<c>""</c>)或“<c>*</c>”，表示匹配所有文件或目录。</para>
/// 	<para>标准搜索模式：多字匹配模式(“<c>*</c>”)和单字匹配模式(“<c>?</c>”)，文件系统基本都支持标准搜索模式。</para>
/// 	<para>正则表达式模式：以反斜杠(“<c>\</c>”)或正斜杠(“<c>/</c>”)或竖线符(“<c>|</c>”)字符为起止的格式。</para>
/// </remarks>
public readonly struct Pattern : IEquatable<Pattern>
{
	#region 静态变量
	private static readonly bool _ignoreCase = OperatingSystem.IsWindows() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();
	#endregion

	#region 私有成员
	private readonly string _text;
	private readonly Regex _regex;
	#endregion

	#region 构造函数
	public Pattern(string text, bool? ignoreCase = null)
	{
		_text = text == "" || text == "*" ? null : text;
		this.IgnoreCase = ignoreCase ?? _ignoreCase;

		if(!string.IsNullOrEmpty(text))
		{
			if(text.Length > 2 &&
			  (text[0] == '|' && text[^1] == '|') ||
			  (text[0] == '/' && text[^1] == '/') ||
			  (text[0] == '\\' && text[^1] == '\\'))
				_regex = new($"^{text[1..^1]}$", RegexOptions.Compiled | (this.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
			else if(text.Contains('*') || text.Contains('?'))
				_regex = new($"^{GetRegex(text)}$", RegexOptions.Compiled | (this.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
		}

		static string GetRegex(ReadOnlySpan<char> text)
		{
			var count = 0;
			var builder = new StringBuilder(text.Length * 2);

			for(int i = 0; i < text.Length; i++)
			{
				if(text[i] == '?')
				{
					++count;
					continue;
				}
				else if(count > 0)
				{
					builder.Append($".{{{count}}}");
					count = 0;
				}

				switch(text[i])
				{
					case '\\':
						builder.Append(@"\\");
						break;
					case '.':
						builder.Append(@"\.");
						break;
					case '*':
						builder.Append(".*");
						break;
					default:
						builder.Append(text[i]);
						break;
				}
			}

			if(count > 0)
				builder.Append($".{{{count}}}");

			return builder.ToString();
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示模式匹配是否区分大小写。</summary>
	public bool IgnoreCase { get; }

	/// <summary>获取一个值，指示模式是否为空。</summary>
	public bool IsEmpty => string.IsNullOrEmpty(_text);
	#endregion

	#region 公共方法
	/// <summary>判断当前模式是否匹配指定路径。</summary>
	/// <param name="path">指定的待匹配路径。</param>
	/// <returns>如果匹配成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public bool Match(string path)
	{
		if(_regex != null)
			return path != null && _regex.IsMatch(path);

		return
			string.IsNullOrEmpty(_text) ||
			string.Equals(path, _text, this.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}
	#endregion

	#region 重写方法
	public bool Equals(Pattern other) => _text == other._text;
	public override bool Equals(object obj) => obj is Pattern other && this.Equals(other);
	public override int GetHashCode() => string.IsNullOrEmpty(_text) ? 0 : _text.GetHashCode();
	public override string ToString() => _text ?? string.Empty;
	#endregion

	#region 符号重写
	public static explicit operator Pattern(string text) => new(text);
	public static implicit operator string(Pattern pattern) => pattern._text;

	public static bool operator ==(Pattern left, Pattern right) => left.Equals(right);
	public static bool operator !=(Pattern left, Pattern right) => !(left == right);
	#endregion
}
