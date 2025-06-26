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
using System.Globalization;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Zongsoft.Data;

/// <summary>
/// 提供 <see cref="Paging"/> 分页设置的解析功能，关于支持的解析语法请查看备注说明。
/// </summary>
/// <remarks>
/// 支持两种解析格式：
/// <list type="bullet">
///		<item>
///			<term>PageIndex|PageSize</term>
///			<description>分页条件：页号与每页记录数之间采用“|”或“@”符号分隔，其中每页记录数可选（忽略则表示系统默认值）。
///				<para>如果页号为数字零或“*”星号，则表示不分页（参见：<seealso cref="Paging.Disabled"/>）。</para>
///			</description>
///		</item>
///		<item>
///			<term>PageIndex/PageCount(TotalCount)</term>
///			<description>分页结果：页号与分页数之间采用“/”符号分隔，之后的记录总数(采用圆括号包裹)可选。</description>
///		</item>
/// </list>
/// </remarks>
public class PagingConverter : TypeConverter
{
	private static readonly Regex _regex_ = new(@"^(?<index>\d+)(([|@](?<size>\d+))|(/(?<count>\d+)(\((?<total>\d+)\))?))?$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1000));

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is string text)
		{
			if(string.IsNullOrEmpty(text))
				return Paging.Page(1);

			if(text == "*" || text == "0")
				return Paging.Disabled;

			var match = _regex_.Match(text);

			if(match.Success && match.Groups["index"].Success)
			{
				if(match.Groups["count"].Success)
				{
					var total = match.Groups["total"].Success ? long.Parse(match.Groups["total"].Value) : 0;

					if(total > 0)
						return new Paging(int.Parse(match.Groups["index"].Value), (int)total / int.Parse(match.Groups["count"].Value)) { TotalCount = total };
					else
						return new Paging(int.Parse(match.Groups["index"].Value));
				}

				return match.Groups["size"].Success ?
				       Paging.Page(int.Parse(match.Groups["index"].Value), int.Parse(match.Groups["size"].Value)) :
				       Paging.Page(int.Parse(match.Groups["index"].Value));
			}
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(value is Paging paging)
		{
			return paging.TotalCount > 0 ?
				$"{paging.PageIndex}/{paging.PageCount}({paging.TotalCount})" :
				$"{paging.PageIndex}|{paging.PageSize}";
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
