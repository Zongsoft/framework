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
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Data;

/// <summary>提供 <see cref="Paging"/> 分页设置的解析功能，关于支持的解析语法请查看备注说明。</summary>
/// <remarks>
/// 支持三种解析格式：
/// <list type="bullet">
///		<item>
///			<term>Limit@Offset</term>
///			<description><c>限制数</c> 与 <c>偏移量</c> 之间采用“<c>@</c>”符号分隔。</description>
///		</item>
///		<item>
///			<term>Index|Size</term>
///			<description>分页条件：<c>页号</c> 与 <c>页大小</c> 之间采用“<c>|</c>”符号分隔，其中 <c>页大小</c> 可选（忽略则表示系统默认值）。
///				<para>如果 <c>页号</c> 为数字零(<c>0</c>)或星号(<c>*</c>)，则表示不分页（参见：<seealso cref="Paging.Disabled"/>）。</para>
///			</description>
///		</item>
///		<item>
///			<term>Index/Count(Total)</term>
///			<description><c>页号</c> 与 <c>分页数</c> 之间采用“<c>/</c>”符号分隔，之后提供采用圆括号包裹的 <c>记录总数</c>。分页数必须大于零。</description>
///		</item>
/// </list>
/// </remarks>
public class PagingConverter : TypeConverter
{
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
			return Paging.Parse(text);

		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if(value is Paging paging)
		{
			return paging.Total > 0 ?
				$"{paging.Index}/{paging.Count}({paging.Total})" :
				$"{paging.Index}|{paging.Size}";
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
