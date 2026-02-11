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
using System.Text.RegularExpressions;

namespace Zongsoft.Data;

partial class Paging
{
	#region 私有变量
	private static readonly Regex _regex_ = new(@"^(?<index>\d+)((/(?<count>\d+)(\((?<total>\d+)\))?))?$",
		RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture,
		TimeSpan.FromMilliseconds(1000));
	#endregion

	public static Paging Parse(ReadOnlySpan<char> text) => TryParse(text, out Paging result) ? result : throw new ArgumentException($"Illegal paging format.");
	public static bool TryParse(ReadOnlySpan<char> text, out Paging result)
	{
		if(text.IsEmpty)
		{
			result = Paging.Disabled;
			return true;
		}

		if(text == "*" || text.Equals(nameof(Disabled), StringComparison.OrdinalIgnoreCase))
		{
			result = Paging.Disabled;
			return true;
		}

		//处理只有“页号”的格式
		if(int.TryParse(text, out var integer))
		{
			result = Paging.Page(integer);
			return true;
		}

		//处理“页号”和“页大小”的格式
		var position = text.IndexOf('|');
		if(position > 0 && int.TryParse(text[..position], out var index) && int.TryParse(text[(position + 1)..], out var size))
		{
			result = Paging.Page(index, size);
			return true;
		}

		//处理“限制数量”和“限制偏移量”的格式
		position = text.IndexOf('@');
		if(position > 0 && int.TryParse(text[..position], out var limit) && long.TryParse(text[(position + 1)..], out var offset))
		{
			result = Paging.Limit(limit, offset);
			return true;
		}

		//最后处理“页号”、“页数”和“总记录数”的格式
		var match = _regex_.Match(text.ToString());

		if(match.Success && match.Groups["index"].Success)
		{
			if(match.Groups["count"].Success)
			{
				var total = match.Groups["total"].Success ? long.Parse(match.Groups["total"].Value) : 0;

				result = total > 0 ?
					new Paging(int.Parse(match.Groups["index"].Value), (int)total / int.Parse(match.Groups["count"].Value)) { Total = total } :
					new Paging(int.Parse(match.Groups["index"].Value));
			}
			else
			{
				result = match.Groups["size"].Success ?
					   Paging.Page(int.Parse(match.Groups["index"].Value), int.Parse(match.Groups["size"].Value)) :
					   Paging.Page(int.Parse(match.Groups["index"].Value));
			}

			return true;
		}

		result = null;
		return false;
	}
}
