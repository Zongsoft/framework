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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Text.Templating;

/// <summary>提供模板求值设置。</summary>
/// <remarks>提示：求值及自动递归期间不得修改设置。</remarks>
public class TemplateEvaluatorOptions
{
	/// <summary>初始化模板求值设置。</summary>
	/// <remarks><see cref="Culture"/> 默认为 <see langword="null"/>，<see cref="Recursive"/> 和 <see cref="Fallback"/> 默认为 <see langword="false"/>，<see cref="MaximumDepth"/> 默认为 <c>64</c>。</remarks>
	public TemplateEvaluatorOptions()
	{
		this.MaximumDepth = 64;
	}

	/// <summary>获取或设置变量值格式化时使用的文化区域。</summary>
	/// <value>格式化所使用的文化区域，默认为 <see langword="null"/>，由 .NET 格式化逻辑使用当前文化区域。</value>
	/// <remarks>
	/// 	<para>此设置作为每次插值的初始格式化文化区域；<see cref="TemplateEvaluator.Formatting"/> 事件可通过 <see cref="TemplateEvaluator.FormattingContext.Culture"/> 为当前插值指定其它文化区域。</para>
	/// 	<para>此设置仅影响格式化，不改变模板语法、变量名称匹配或成员导航的参数类型。</para>
	/// </remarks>
	public CultureInfo Culture { get; set; }

	/// <summary>获取或设置是否将求得的字符串继续作为模板递归评估。</summary>
	/// <value>允许递归评估则为 <see langword="true"/>，否则为 <see langword="false"/>；默认为 <see langword="false"/>。</value>
	/// <remarks>
	/// 	<para>启用后，对取值及 <see cref="TemplateEvaluator.Resolved"/> 事件处理后得到的每个字符串执行子模板求值，包括不含变量引用的普通字符串及动态索引变量的字符串值；非字符串值保持原值。</para>
	/// 	<para>关闭时取得的字符串作为字面数据使用。索引中的引号常量及格式化事件生成的文本不参与自动递归。</para>
	/// 	<para>递归沿用当前求值设置，并受 <see cref="MaximumDepth"/> 限制。</para>
	/// </remarks>
	public bool Recursive { get; set; }

	/// <summary>获取或设置是否允许变量回退，默认为 <see langword="false"/>。</summary>
	/// <remarks>
	/// 	<para>启用后按命名空间层级及来源顺序查询，最后允许来源自身已声明的默认值；递归模板使用相同设置。</para>
	/// 	<para>找到 <see langword="null"/>、空字符串、<see langword="false"/> 或零即结束查找，不因值无效而回退。</para>
	/// </remarks>
	public bool Fallback { get; set; }

	/// <summary>获取或设置自动递归求值允许的最大模板层数。</summary>
	/// <value>大于 <c>0</c> 的最大模板层数，默认为 <c>64</c>。</value>
	/// <remarks>
	/// 	<para>根模板计为第 <c>1</c> 层；每次将字符串作为子模板求值时增加一层，包括不含变量引用的字符串。相邻变量引用及索引语法嵌套本身不累积模板层数。</para>
	/// 	<para>仅在 <see cref="Recursive"/> 启用时限制自动递归。尝试进入超过上限的子模板会引发 <see cref="TemplateEvaluationException"/>，其 <see cref="TemplateEvaluationException.Code"/> 为 <c>DepthExceeded</c>。</para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">设置的值小于或等于 <c>0</c>。</exception>
	public int MaximumDepth
	{
		get;
		set => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
}
