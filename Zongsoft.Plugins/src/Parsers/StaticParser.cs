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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;

namespace Zongsoft.Plugins.Parsers
{
	public class StaticParser : Parser
	{
		public override object Parse(ParserContext context)
		{
			if(string.IsNullOrWhiteSpace(context.Text) || string.Equals(context.Text, "null", StringComparison.OrdinalIgnoreCase))
				return null;

			var member = PluginUtility.GetStaticMember(context.Text);

			if(member != null)
			{
				switch(member.MemberType)
				{
					case (MemberTypes.Field):
						return ((FieldInfo)member).GetValue(null);
					case (MemberTypes.Property):
						return ((PropertyInfo)member).GetValue(null, null);
				}
			}

			return null;
		}
	}
}
