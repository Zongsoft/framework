﻿/*
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
