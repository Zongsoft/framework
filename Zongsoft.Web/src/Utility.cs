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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text.RegularExpressions;

namespace Zongsoft.Web
{
	internal static class Utility
	{
		#region 私有变量
		/*
\s*
(?<part>
	(\*)|
	(\([^\(\)]+\))|
	([^-]+)
)
\s*-?
		 */
		private static readonly Regex _regex = new Regex(@"\s*(?<part>(\*)|(\([^\(\)]+\))|([^-]+))\s*-?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
		#endregion

		public static string[] Slice(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return new string[0];

			var matches = _regex.Matches(text);
			var result = new string[matches.Count];

			for(var i = 0; i < matches.Count; i++)
			{
				if(matches[i].Success)
				{
					result[i] = matches[i].Groups["part"].Value;

					if(result[i] == "*")
						result[i] = string.Empty;
				}
				else
				{
					result[i] = string.Empty;
				}
			}

			return result;
		}
	}
}
