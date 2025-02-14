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

namespace Zongsoft.Configuration.Profiles;

public abstract class ProfileItem
{
	#region 构造函数
	protected ProfileItem(Profile profile, int lineNumber = -1)
	{
		this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));
		this.LineNumber = Math.Max(lineNumber, -1);
	}

	protected ProfileItem(ProfileSection section, int lineNumber = -1)
	{
		this.Section = section ?? throw new ArgumentNullException(nameof(section));
		this.Profile = section.Profile;
		this.LineNumber = Math.Max(lineNumber, -1);
	}
	#endregion

	#region 公共属性
	public Profile Profile { get; }
	public ProfileSection Section { get; }
	public abstract ProfileItemType ItemType { get; }
	public int LineNumber { get; }
	#endregion
}
