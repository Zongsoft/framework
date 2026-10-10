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

using Zongsoft.Common;
using Zongsoft.Expressions.Tokenization;

namespace Zongsoft.Configuration.Profiles;

/// <summary>提供配置、章节和条目的变量视图扩展。</summary>
/// <remarks>
/// 	<para>视图持有所选配置对象，每次查询读取其当前有效条目，不复制数据、不展开模板、不转换类型，也不提供额外的并发同步。</para>
/// 	<para>
/// 		根级条目属于默认命名空间；各级章节名称以点号连接，章节名称内的点号原样保留。
/// 		命名空间的每个点分段必须符合 <c>[A-Za-z_][A-Za-z0-9_]*</c>，不符合规则的章节及其子章节不提供变量。
/// 		条目名称中的点号和连字符替换为下划线后，必须符合相同的标识符规则，否则不提供该变量。
/// 	</para>
/// 	<para>
/// 		查询名称与命名空间按 <see cref="StringComparison.OrdinalIgnoreCase"/> 比较；查询参数不裁剪、不替换字符。
/// 		命名空间为 <see langword="null"/> 或空字符串时查询默认命名空间，非空命名空间不会回退到父级或默认命名空间。
/// 		查询名称为 <see langword="null"/> 时抛出 <see cref="ArgumentNullException"/>，其它非法查询返回 <see langword="false"/>。
/// 	</para>
/// 	<para>存在且值为 <see langword="null"/> 的变量仍返回查询成功；仅在查询到多个条目映射的同名变量时抛出 <see cref="ProfileException"/>，其它变量不受影响。</para>
/// </remarks>
public static class ProfileExtension
{
	#region 公共方法
	/// <summary>将指定配置转换为包含根级条目及全部章节的变量视图。</summary>
	/// <param name="profile">作为变量来源的配置，不能为 <see langword="null"/>。</param>
	/// <returns>读取该配置当前有效条目的变量视图，包含已经合并的导入结果。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="profile"/> 为 <see langword="null"/>。</exception>
	/// <remarks>
	/// 	<para>查询遵循配置已有的导入覆盖结果，不回到条目的原始声明文件查找，也不读取尚未加载的内容。</para>
	/// 	<para>修改值、添加、替换或删除条目和章节后，后续查询立即反映这些变化。</para>
	/// </remarks>
	public static IVariables ToVariables(this Profile profile)
	{
		ArgumentNullException.ThrowIfNull(profile);
		return new ProfileVariables(profile);
	}

	/// <summary>将指定章节转换为包含其自身条目及全部子章节的变量视图。</summary>
	/// <param name="section">作为变量来源的章节，不能为 <see langword="null"/>。</param>
	/// <returns>读取该章节当前子树的变量视图，保留包含祖先章节名称的完整命名空间。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="section"/> 为 <see langword="null"/>。</exception>
	/// <remarks>
	/// 	<para>章节不会重新定位到默认命名空间。例如 <c>[io rustfs]</c> 的条目仍通过 <c>io.rustfs</c> 查询。</para>
	/// 	<para>不提供父章节、兄弟章节或根级条目；视图保留所选章节对象，原集合移除或替换该对象不会使视图转向其它章节。</para>
	/// </remarks>
	public static IVariables ToVariables(this ProfileSection section)
	{
		ArgumentNullException.ThrowIfNull(section);
		return new SectionVariables(section);
	}

	/// <summary>将指定条目转换为仅提供该条目的变量视图。</summary>
	/// <param name="entry">作为变量来源的条目，不能为 <see langword="null"/>。</param>
	/// <returns>读取该条目当前值的变量视图，保留其所属章节的完整命名空间。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entry"/> 为 <see langword="null"/>。</exception>
	/// <remarks>
	/// 	<para>视图不提供其它条目；名称或所属命名空间不合法时不提供任何变量。</para>
	/// 	<para>视图始终读取所选条目对象，原集合移除或替换该对象不会改变视图的来源。</para>
	/// </remarks>
	public static IVariables ToVariables(this ProfileEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return new EntryVariables(entry);
	}
	#endregion

	#region 私有方法
	private static bool CanLookup(string @namespace, string name)
	{
		ArgumentNullException.ThrowIfNull(name);
		return IdentifierTokenizer.IsIdentifier(name) && (string.IsNullOrEmpty(@namespace) || IsNamespace(@namespace));
	}

	private static bool IsNamespace(ReadOnlySpan<char> text)
	{
		while(!text.IsEmpty)
		{
			var index = text.IndexOf('.');

			if(index < 0)
				return IdentifierTokenizer.IsIdentifier(text);

			if(!IdentifierTokenizer.IsIdentifier(text[..index]))
				return false;

			text = text[(index + 1)..];
		}

		return false;
	}

	private static string GetNamespace(ProfileSection section)
	{
		if(section == null)
			return string.Empty;

		if(!IsNamespace(section.Name))
			return null;

		var parent = GetNamespace(section.Section);
		return parent == null ? null : parent.Length == 0 ? section.Name : $"{parent}.{section.Name}";
	}

	private static bool MatchesName(ReadOnlySpan<char> source, ReadOnlySpan<char> name)
	{
		if(source.Length != name.Length)
			return false;

		for(int i = 0; i < source.Length; i++)
		{
			var character = source[i] is '.' or '-' ? '_' : source[i];

			//查询名称已经过验证，仅允许 ASCII 字母参与忽略大小写比较。
			if(character != name[i] && (!char.IsAsciiLetter(character) || char.ToUpperInvariant(character) != char.ToUpperInvariant(name[i])))
				return false;
		}

		return true;
	}

	private static void Find(ProfileEntryCollection entries, string name, ref ProfileEntry result)
	{
		foreach(var entry in entries)
		{
			if(!MatchesName(entry.Name, name))
				continue;

			if(result != null)
			{
				var @namespace = GetNamespace(entry.Section);
				var variable = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}:{name}";
				throw new ProfileException(string.Format(Properties.Resources.Profiles_VariableAmbiguous_Message, variable));
			}

			result = entry;
		}
	}

	private static void Find(ProfileSectionCollection sections, ReadOnlySpan<char> @namespace, string name, ref ProfileEntry result)
	{
		foreach(var section in sections)
			Find(section, section.Name, @namespace, name, ref result);
	}

	private static void Find(ProfileSection section, string prefix, ReadOnlySpan<char> @namespace, string name, ref ProfileEntry result)
	{
		if(prefix == null || !IsNamespace(prefix) || !@namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			return;

		if(@namespace.Length == prefix.Length)
			Find(section.Entries, name, ref result);
		else if(@namespace[prefix.Length] == '.')
			Find(section.Sections, @namespace[(prefix.Length + 1)..], name, ref result);
	}
	#endregion

	#region 嵌套子类
	private sealed class ProfileVariables(Profile profile) : IVariables
	{
		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string @namespace, string name, out object value)
		{
			value = null;

			if(!CanLookup(@namespace, name))
				return false;

			ProfileEntry entry = null;

			if(string.IsNullOrEmpty(@namespace))
				Find(profile.Entries, name, ref entry);
			else
				Find(profile.Sections, @namespace, name, ref entry);

			value = entry?.Value;
			return entry != null;
		}
	}

	private sealed class SectionVariables(ProfileSection section) : IVariables
	{
		private readonly string _namespace = GetNamespace(section);

		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string @namespace, string name, out object value)
		{
			value = null;

			if(!CanLookup(@namespace, name))
				return false;

			ProfileEntry entry = null;
			Find(section, _namespace, @namespace, name, ref entry);
			value = entry?.Value;
			return entry != null;
		}
	}

	private sealed class EntryVariables(ProfileEntry entry) : IVariables
	{
		private readonly string _namespace = GetNamespace(entry.Section);

		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, out value);
		public bool TryGetValue(string @namespace, string name, out object value)
		{
			value = null;

			if(!CanLookup(@namespace, name) || _namespace == null ||
			   !string.Equals(_namespace, @namespace ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
			   !MatchesName(entry.Name, name))
				return false;

			value = entry.Value;
			return true;
		}
	}
	#endregion
}
