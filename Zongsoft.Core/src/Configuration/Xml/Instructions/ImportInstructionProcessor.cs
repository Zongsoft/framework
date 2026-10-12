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

namespace Zongsoft.Configuration.Xml.Instructions;

/// <summary>按声明位置导入 XML 配置。</summary>
public sealed class ImportInstructionProcessor : XmlConfigurationInstructionProcessor
{
	/// <summary>获取内置导入处理器的共享实例。</summary>
	public static readonly ImportInstructionProcessor Instance = new();
	private ImportInstructionProcessor() : base(XmlConfigurationInstructionOptions.ImportOptions.NAME) { }

	/// <inheritdoc />
	/// <param name="context">本次导入指令的上下文。</param>
	public override void Process(XmlConfigurationInstructionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		if(context.XmlDepth != 0 && (context.XmlDepth != 2 || context.OptionPath == null))
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportPositionInvalid_Message, context.Instruction);

		var attributes = context.GetAttributes();
		if(!attributes.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportPathRequired_Message, context.Instruction);

		var required = true;
		if(context.Behavior == XmlConfigurationInstructionBehavior.None && attributes.TryGetValue("required", out var text) && !bool.TryParse(text, out required))
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportRequiredInvalid_Message, context.Instruction, text);

		context.Import(path, required, context.Options is XmlConfigurationInstructionOptions.ImportOptions options ? options.MaximumDepth : XmlConfigurationInstructionOptions.ImportOptions.DEFAULT_MAXIMUM_DEPTH);
	}
}
