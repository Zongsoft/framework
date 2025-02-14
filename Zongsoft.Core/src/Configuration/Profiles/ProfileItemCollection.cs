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
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration.Profiles;

public class ProfileItemCollection<TItem> : Collection<TItem> where TItem : ProfileItem
{
	#region 成员字段
	private readonly Profile _profile;
	private readonly ProfileSection _section;
	#endregion

	#region 构造函数
	protected ProfileItemCollection(Profile profile)
	{
		_profile = profile ?? throw new ArgumentNullException(nameof(profile));
	}

	protected ProfileItemCollection(ProfileSection section)
	{
		_section = section ?? throw new ArgumentNullException(nameof(section));
		_profile = section.Profile;
	}
	#endregion

	#region 内部属性
	internal Profile Profile => _profile;
	internal ProfileSection Section => _section;
	#endregion
}
