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

namespace Zongsoft.Configuration.Xml.Instructions;

/// <summary>按声明位置导入 XML 配置。</summary>
public sealed class ImportInstructionProcessor : XmlConfigurationInstructionProcessor
{
	#region 单例字段
	/// <summary>获取内置导入处理器的共享实例。</summary>
	public static readonly ImportInstructionProcessor Instance = new();
	#endregion

	#region 私有构造
	private ImportInstructionProcessor() : base(XmlConfigurationInstructionOptions.ImportOptions.NAME) { }
	#endregion

	#region 重写方法
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
	#endregion
}
