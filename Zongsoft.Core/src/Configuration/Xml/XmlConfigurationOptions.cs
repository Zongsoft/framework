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

namespace Zongsoft.Configuration.Xml;

/// <summary>提供 XML 配置加载和处理指令的选项。</summary>
public class XmlConfigurationOptions
{
	#region 构造函数
	/// <summary>初始化 <see cref="XmlConfigurationOptions"/> 类的新实例。</summary>
	public XmlConfigurationOptions()
	{
		this.Instructions = new(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令选项集合；未配置的指令采用其内置默认设置。</summary>
	public XmlConfigurationInstructionOptionsCollection Instructions { get; }

	/// <summary>获取或设置配置读取前的回调，根配置和导入配置均触发。</summary>
	/// <remarks>文件打开和循环、深度检查通过后，解析内容前触发；此时上下文的 <see cref="XmlConfigurationContext.Configuration"/> 为空字典。</remarks>
	public Action<XmlConfigurationContext> Loading { get; set; }

	/// <summary>获取或设置配置读取成功后的回调，根配置和导入配置均触发。</summary>
	/// <remarks>在合并到引用者之前触发，对当前字典的修改参与合并。异常终止加载。</remarks>
	public Action<XmlConfigurationContext> Loaded { get; set; }
	#endregion

	#region 内部方法
	//复制指令集合和各选项；委托捕获的外部状态仍由调用方管理。
	internal XmlConfigurationOptions Clone()
	{
		var options = new XmlConfigurationOptions()
		{
			Loading = this.Loading,
			Loaded = this.Loaded,
		};

		options.Instructions.Processing = this.Instructions.Processing;
		options.Instructions.Processed = this.Instructions.Processed;

		foreach(var instruction in this.Instructions)
			options.Instructions.Add(instruction.Clone());

		return options;
	}
	#endregion
}
