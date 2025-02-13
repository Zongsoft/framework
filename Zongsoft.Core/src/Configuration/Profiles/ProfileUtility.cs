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

public static class ProfileUtility
{
	public static IReadOnlyList<ProfileItem> GetItems(this Profile profile)
	{
		if(profile == null)
			return [];

		var list = new List<ProfileItem>(profile.Comments.Count + profile.Entries.Count + profile.Sections.Count);

		list.AddRange(profile.Comments);
		list.AddRange(profile.Entries);
		list.AddRange(profile.Sections);

		return list;
	}

	public static IReadOnlyList<ProfileItem> GetItems(this ProfileSection section)
	{
		if(section == null)
			return [];

		var list = new List<ProfileItem>(section.Comments.Count + section.Entries.Count + section.Sections.Count);

		list.AddRange(section.Comments);
		list.AddRange(section.Entries);
		list.AddRange(section.Sections);

		return list;
	}
}
