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
using System.Text.RegularExpressions;

namespace Zongsoft.Data;

partial class Paging
{
	#region 私有变量
	private static readonly Regex _regex_ = new(@"^(?<index>\d+)/(?<count>\d+)\((?<total>\d+)\)$",
		RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture,
		TimeSpan.FromMilliseconds(1000));
	#endregion

	public static Paging Parse(ReadOnlySpan<char> text) => TryParse(text, out Paging result) ? result : throw new ArgumentException(Properties.Resources.Paging_InvalidFormat_Message);
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
			result = integer > 0 ? Paging.Page(integer) : Paging.Disabled;
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

		if(match.Success &&
		   int.TryParse(match.Groups["index"].ValueSpan, out index) &&
		   int.TryParse(match.Groups["count"].ValueSpan, out var count) &&
		   long.TryParse(match.Groups["total"].ValueSpan, out var total))
		{
			result = new(index, (int)(total / count)) { Total = total };
			return true;
		}

		result = null;
		return false;
	}
}
