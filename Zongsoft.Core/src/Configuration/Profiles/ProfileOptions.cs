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

namespace Zongsoft.Configuration.Profiles;

public class ProfileOptions
{
	#region 构造函数
	public ProfileOptions(bool preserveBlanks = true)
	{
		this.PreserveBlanks = preserveBlanks;
		this.Directives = new();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，指示是否保留空行。</summary>
	public bool PreserveBlanks { get; set; }

	/// <summary>获取指令选项集合；未配置的指令采用其内置默认设置。</summary>
	public ProfileDirectiveOptionsCollection Directives { get; }

	/// <summary>获取或设置配置读取前的回调，根配置和导入配置均触发。</summary>
	/// <remarks>文件打开和循环、深度检查通过后，解析内容前触发；此时上下文的 Profile 为空。</remarks>
	public Action<ProfileContext> Loading { get; set; }

	/// <summary>获取或设置配置读取成功后的回调，根配置和导入配置均触发。</summary>
	/// <remarks>导入配置已合并到引用者。异常终止加载，不回滚已完成的合并和通知。</remarks>
	public Action<ProfileContext> Loaded { get; set; }

	/// <summary>获取或设置指令处理前的回调，可修改执行参数或标记已处理。</summary>
	/// <remarks>指令名称及原始参数已识别；忽略或禁止的指令不触发该回调。</remarks>
	public Action<ProfileDirectiveContext> DirectiveProcessing { get; set; }

	/// <summary>获取或设置指令成功处理后的回调。</summary>
	/// <remarks>本条指令的递归处理及合并均已完成；忽略、禁止或处理失败的指令不触发。</remarks>
	public Action<ProfileDirectiveContext> DirectiveProcessed { get; set; }
	#endregion

	#region 内部方法
	//复制指令集合和各选项；委托捕获的外部状态仍由调用方管理。
	internal ProfileOptions Clone()
	{
		var options = new ProfileOptions(this.PreserveBlanks)
		{
			Loading = this.Loading,
			Loaded = this.Loaded,
			DirectiveProcessing = this.DirectiveProcessing,
			DirectiveProcessed = this.DirectiveProcessed,
		};

		foreach(var directive in this.Directives)
			options.Directives.Add(directive.Clone());

		return options;
	}
	#endregion
}
