/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Configuration.Profiles
{
	internal static class ProfileItemCollectionExtension
	{
		public static ProfileComment Add(this ICollection<ProfileComment> comments, string comment, int lineNumber = -1)
		{
			if(comment == null)
				return null;

			var item = new ProfileComment(comment, lineNumber);
			comments.Add(item);
			return item;
		}

		public static ProfileSection Add(this Collections.INamedCollection<ProfileSection> sections, string name, int lineNumber = -1)
		{
			var item = new ProfileSection(name, lineNumber);
			sections.Add(item);
			return item;
		}

		public static ProfileEntry Add(this Collections.INamedCollection<ProfileEntry> entries, string name, string value = null)
		{
			var item = new ProfileEntry(name, value);
			entries.Add(item);
			return item;
		}

		public static ProfileEntry Add(this Collections.INamedCollection<ProfileEntry> entries, int lineNumber, string name, string value = null)
		{
			var item = new ProfileEntry(lineNumber, name, value);
			entries.Add(item);
			return item;
		}
	}
}
