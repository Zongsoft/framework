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

namespace Zongsoft.Configuration.Profiles;

public class ProfileEntry : ProfileItem
{
	#region 构造函数
	public ProfileEntry(Profile profile, string name, string value = null) : this(profile, -1, name, value) { }
	public ProfileEntry(Profile profile, int lineNumber, string name, string value = null) : base(profile, lineNumber)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		this.Value = value?.Trim();
	}

	public ProfileEntry(ProfileSection section, string name, string value = null) : this(section, -1, name, value) { }
	public ProfileEntry(ProfileSection section, int lineNumber, string name, string value = null) : base(section, lineNumber)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		this.Value = value?.Trim();
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Value { get; set; }
	public override ProfileItemType ItemType => ProfileItemType.Entry;
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Value) ? this.Name : $"{this.Name}={this.Value}";
	#endregion
}
