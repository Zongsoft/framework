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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

public class ProfileSection : ProfileItem, IEnumerable<ProfileItem>
{
	#region 静态常量
	private static readonly System.Buffers.SearchValues<char> _illegal_characters_ = System.Buffers.SearchValues.Create("/\\|*?=%^&<>{}");
	#endregion

	#region 构造函数
	public ProfileSection(Profile profile, string name, int lineNumber = -1) : base(profile, lineNumber)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(name.AsSpan().IndexOfAny(_illegal_characters_) >= 0)
			throw new ArgumentException(string.Format(Properties.Resources.Profiles_InvalidSectionName_Message, name));

		this.Name = name.Trim();
		this.FullName = this.Name;
		this.Entries = new(this);
		this.Comments = new(this);
		this.Sections = new(this);
	}

	public ProfileSection(ProfileSection section, string name, int lineNumber = -1) : base(section, lineNumber)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(name.AsSpan().IndexOfAny(_illegal_characters_) >= 0)
			throw new ArgumentException(string.Format(Properties.Resources.Profiles_InvalidSectionName_Message, name));

		this.Name = name.Trim();
		this.FullName = section.FullName + ' ' + this.Name;
		this.Entries = new(this);
		this.Comments = new(this);
		this.Sections = new(this);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string FullName { get; }
	public ProfileEntryCollection Entries { get; }
	public ProfileCommentCollection Comments { get; }
	public ProfileSectionCollection Sections { get; }
	public override ProfileItemType ItemType => ProfileItemType.Section;
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.FullName}]";
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<ProfileItem> GetEnumerator() => this.GetItems().GetEnumerator();
	#endregion

	#region 公共方法
	public string GetEntryValue(string name)
	{
		if(this.Entries.TryGetValue(name, out var entry))
			return entry.Value;

		return null;
	}

	public void SetEntryValue(string name, string value)
	{
		if(this.Entries.TryGetValue(name, out var entry))
			entry.Value = value;
		else
			this.Entries.Add(name, value);
	}
	#endregion
}
