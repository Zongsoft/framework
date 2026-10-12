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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Xml;

/// <summary>提供一条 XML 处理指令的执行上下文。</summary>
public sealed class XmlConfigurationInstructionContext : XmlConfigurationContext
{
	#region 成员字段
	private readonly XmlConfigurationSession _session;
	private readonly XmlConfigurationSession.File _file;
	private XmlConfigurationInstruction _currentInstruction;
	#endregion

	#region 构造函数
	internal XmlConfigurationInstructionContext(XmlConfigurationSession session, XmlConfigurationSession.File file, XmlConfigurationInstruction instruction, string optionPath, int xmlDepth, XmlConfigurationInstructionOptions options) :
		base(file.Context.FilePath, file.Context.Depth, file.Context.Referer, file.Context.Configuration)
	{
		_session = session;
		_file = file;
		this.Instruction = instruction;
		_currentInstruction = instruction;
		this.OptionPath = optionPath;
		this.XmlDepth = xmlDepth;
		this.Behavior = options.Behavior;
		this.Options = options.Clone();
	}
	#endregion

	#region 公共属性
	/// <summary>获取不可变的原始处理指令及其来源位置。</summary>
	public XmlConfigurationInstruction Instruction { get; }
	/// <summary>获取或设置本次执行的数据，不裁剪空白，修改不会改变 <see cref="Instruction"/>。</summary>
	public string Data
	{
		get => _currentInstruction.Data;
		set => _currentInstruction = new(this.Instruction.Name, value, this.Instruction.Path, this.FilePath, this.Instruction.LineNumber, this.Instruction.LinePosition);
	}
	/// <summary>获取所属 <c>option</c> 的规范化配置路径；配置根路径为空字符串，文件级指令为 <see langword="null"/>。</summary>
	public string OptionPath { get; }
	/// <summary>获取或设置是否已接管本条指令，前置回调设为 <see langword="true"/> 时跳过处理器。</summary>
	public bool Handled { get; set; }
	/// <summary>获取已经确定的行为，修改选项副本不会改变本条指令的行为。</summary>
	public XmlConfigurationInstructionBehavior Behavior { get; }
	/// <summary>获取本条指令的独立选项副本。</summary>
	public XmlConfigurationInstructionOptions Options { get; }
	#endregion

	#region 公共方法
	/// <summary>将当前执行数据解析为属性式参数。</summary>
	/// <returns>名称不区分大小写的只读属性集合。</returns>
	/// <exception cref="FormatException"><see cref="Data"/> 不符合属性式参数语法。</exception>
	public IReadOnlyDictionary<string, string> GetAttributes() => _currentInstruction.GetAttributes();
	/// <summary>尝试将当前执行数据解析为属性式参数。</summary>
	/// <param name="attributes">成功时为只读属性集合，失败时为 <see langword="null"/>。</param>
	/// <returns>属性语法有效则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	public bool TryGetAttributes(out IReadOnlyDictionary<string, string> attributes) => _currentInstruction.TryGetAttributes(out attributes);
	#endregion

	#region 内部成员
	internal int XmlDepth { get; }
	internal void Import(string path, bool required, int maximumDepth) => _session.Import(_file, this, path, required, maximumDepth);
	#endregion
}
