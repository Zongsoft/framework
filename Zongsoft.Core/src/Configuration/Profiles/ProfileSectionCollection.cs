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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

public class ProfileSectionCollection : ProfileItemCollection<ProfileSection>
{
	#region 成员字段
	private readonly Dictionary<string, ProfileSection> _dictionary;
	#endregion

	#region 构造函数
	public ProfileSectionCollection(Profile profile) : base(profile) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
	public ProfileSectionCollection(ProfileSection section) : base(section) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public ProfileSection this[string name] => name != null && _dictionary.TryGetValue(name, out var section) ? section : null;
	#endregion

	#region 公共方法
	public bool Contains(string name) => name != null && _dictionary.ContainsKey(name);
	public bool TryGetValue(string name, out ProfileSection section) => _dictionary.TryGetValue(name, out section);

	public ProfileSection Find(string path)
	{
		if(string.IsNullOrEmpty(path))
			return null;

		var dictionary = _dictionary;
		var parts = path.Split(['/', ' ', '\t'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			if(!dictionary.TryGetValue(parts[i], out var section))
				return null;

			if(i == parts.Length - 1)
				return section;

			dictionary = section.Sections._dictionary;
		}

		return null;
	}

	public bool Remove(string name, out ProfileSection section)
	{
		if(!_dictionary.TryGetValue(name, out section))
			return false;

		this.Remove(section);
		return true;
	}

	public ProfileSection Add(string name, int lineNumber = -1)
	{
		ProfileSection section = this.Section == null ? new(this.Profile, name, lineNumber) : new(this.Section, name, lineNumber);
		this.Add(section);
		return section;
	}
	#endregion

	#region 内部方法
	internal ProfileSection GetOrAdd(string name, int lineNumber = -1)
	{
		if(_dictionary.TryGetValue(name, out var section))
			return section;

		section = this.Section == null ? new(this.Profile, name, lineNumber) : new(this.Section, name, lineNumber);
		_dictionary.Add(section.Name, section);
		this.Items.Add(section);
		return section;
	}
	#endregion

	#region 重写方法
	protected override void InsertItem(int index, ProfileSection section)
	{
		this.Profile.VerifyOwner(section, this.Section);
		_dictionary.Add(section.Name, section);
		this.Items.Insert(index, section);
		this.Profile.DeclareTree(section);
	}

	protected override void SetItem(int index, ProfileSection section)
	{
		this.Profile.VerifyOwner(section, this.Section);
		var previous = this.Items[index];
		VerifyRemoval(previous);

		if(ReferenceEquals(previous, section))
			return;

		if(!string.Equals(previous.Name, section.Name, StringComparison.OrdinalIgnoreCase) && _dictionary.ContainsKey(section.Name))
			throw new ArgumentException(Properties.Resources.Profiles_SectionDuplicate, nameof(section));

		this.Items[index] = section;
		_dictionary.Remove(previous.Name);
		_dictionary.Add(section.Name, section);
		this.Profile.ReplaceTree(previous, section);
	}

	protected override void RemoveItem(int index)
	{
		var section = this.Items[index];
		VerifyRemoval(section);
		this.Items.RemoveAt(index);
		_dictionary.Remove(section.Name);
		this.Profile.RemoveDeclaration(section);
	}

	protected override void ClearItems()
	{
		foreach(var section in this.Items)
			VerifyRemoval(section);

		foreach(var section in this.Items)
			this.Profile.RemoveDeclaration(section);

		this.Items.Clear();
		_dictionary.Clear();
	}
	#endregion

	#region 私有方法
	private static void VerifyRemoval(ProfileSection section)
	{
		foreach(var entry in section.Entries)
			section.Profile.VerifyOwner(entry, section);

		foreach(var child in section.Sections)
			VerifyRemoval(child);
	}
	#endregion
}
