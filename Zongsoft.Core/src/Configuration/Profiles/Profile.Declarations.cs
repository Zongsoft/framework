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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

partial class Profile
{
	#region 成员字段
	private readonly List<Statement> _statements = [];
	private readonly List<Profile> _imports = [];
	private List<string> _baseline;
	private bool _writing;
	#endregion

	#region 内部属性
	internal IReadOnlyList<Statement> Statements => _statements;
	internal IReadOnlyList<Profile> Imports => _imports;
	internal bool IsModified => _baseline == null || !this.Snapshot().SequenceEqual(_baseline);
	#endregion

	#region 内部方法
	internal void BeginWrite()
	{
		if(_writing)
			throw new InvalidOperationException(Properties.Resources.Profiles_AlreadySaving);

		_writing = true;
	}

	internal void EndWrite() => _writing = false;

	internal void BeginRead() => _baseline = [];

	internal void CompleteRead(int[] blanks)
	{
		this.Blanks = blanks;
		AppendBlanks(_baseline, blanks);
	}

	internal void Import(Profile profile)
	{
		foreach(var entry in profile.Entries)
			this.Entries.Import(entry);

		foreach(var section in profile.Sections)
			ImportSection(this.Sections, section);

		_imports.Add(profile);
	}

	internal void Declare(ProfileItem item, bool original = false)
	{
		if(!this.IsAttached(item.Section))
			return;

		var statement = new Statement(item);
		_statements.Add(statement);

		if(original)
			statement.Snapshot(_baseline);
	}

	internal void DeclareSection(ProfileSection section, int lineNumber, bool original)
	{
		var statement = new Statement(section, lineNumber);
		_statements.Add(statement);

		if(original)
			statement.Snapshot(_baseline);
	}

	internal void DeclareTree(ProfileSection section)
	{
		if(!this.IsAttached(section))
			return;

		this.Declare(section);

		foreach(var item in section.GetItems())
		{
			if(item.Profile != this)
				continue;

			if(item is ProfileSection child)
				this.DeclareTree(child);
			else
				this.Declare(item);
		}
	}

	internal void ReplaceDeclaration(ProfileItem previous, ProfileItem current)
	{
		var index = _statements.FindIndex(statement => ReferenceEquals(statement.Item, previous));

		if(index >= 0)
			_statements[index] = new Statement(current);
		else
			this.Declare(current);
	}

	internal void ReplaceTree(ProfileSection previous, ProfileSection current)
	{
		var index = _statements.FindIndex(statement => BelongsTo(statement.Section, previous));
		this.RemoveDeclaration(previous);
		var start = _statements.Count;
		this.DeclareTree(current);

		if(index >= 0 && index < start)
		{
			var statements = _statements.GetRange(start, _statements.Count - start);
			_statements.RemoveRange(start, statements.Count);
			_statements.InsertRange(index, statements);
		}
	}

	internal void RemoveDeclaration(ProfileItem item)
	{
		if(item is ProfileSection section)
			_statements.RemoveAll(statement => BelongsTo(statement.Section, section));
		else
			_statements.RemoveAll(statement => ReferenceEquals(statement.Item, item));
	}

	internal void VerifyOwner(ProfileItem item, ProfileSection section)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		if(item.Profile != this || item.Section != section)
			throw new InvalidOperationException(Properties.Resources.Profiles_SourceEdit);
	}

	internal string[] Snapshot()
	{
		List<string> values = [];

		foreach(var statement in _statements)
			statement.Snapshot(values);

		AppendBlanks(values, this.Blanks);
		return [.. values];
	}

	internal void AcceptChanges(string[] snapshot) => _baseline = new(snapshot);
	#endregion

	#region 私有方法
	private static void ImportSection(ProfileSectionCollection sections, ProfileSection source)
	{
		var target = sections.GetOrAdd(source.Name);

		foreach(var entry in source.Entries)
			target.Entries.Import(entry);

		foreach(var child in source.Sections)
			ImportSection(target.Sections, child);
	}

	private bool IsAttached(ProfileSection section)
	{
		if(section == null)
			return true;

		var sections = section.Section == null ? this.Sections : section.Section.Sections;
		return sections.Contains(section) && this.IsAttached(section.Section);
	}

	private static bool BelongsTo(ProfileSection section, ProfileSection ancestor)
	{
		while(section != null)
		{
			if(section == ancestor)
				return true;

			section = section.Section;
		}

		return false;
	}

	private static void AppendBlanks(List<string> values, int[] blanks)
	{
		values.Add("blanks");

		if(blanks != null)
		{
			foreach(var blank in blanks)
				values.Add(blank.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
	}
	#endregion

	#region 嵌套类型
	internal sealed class Statement
	{
		#region 构造函数
		public Statement(ProfileItem item)
		{
			this.Item = item;
			this.Section = item is ProfileSection section ? section : item?.Section;
			this.IsSection = item is ProfileSection;
			this.LineNumber = item?.LineNumber ?? -1;
		}

		public Statement(ProfileSection section, int lineNumber)
		{
			this.Item = section;
			this.Section = section;
			this.IsSection = true;
			this.LineNumber = lineNumber;
		}
		#endregion

		#region 公共属性
		public ProfileItem Item { get; }
		public ProfileSection Section { get; }
		public bool IsSection { get; }
		public int LineNumber { get; }
		#endregion

		#region 公共方法
		public void Snapshot(List<string> values)
		{
			values.Add(this.IsSection ? "section" : this.Item.ItemType.ToString());
			values.Add(this.Section?.FullName);

			if(this.Item is ProfileEntry entry)
			{
				values.Add(entry.Name);
				values.Add(entry.Value);
			}
			else if(this.Item is ProfileComment comment)
			{
				values.Add(comment.Lines.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
				values.AddRange(comment.Lines);
			}
		}
		#endregion
	}
	#endregion
}
