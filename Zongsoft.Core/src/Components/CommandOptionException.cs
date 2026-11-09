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

namespace Zongsoft.Components;

[Serializable]
public class CommandOptionException : CommandException
{
	#region 构造函数
	public CommandOptionException(string optionName) : this(optionName, null) { }
	public CommandOptionException(string optionName, string message) : base(message)
	{
		if(string.IsNullOrEmpty(optionName))
			throw new ArgumentNullException(nameof(optionName));

		this.OptionName = optionName.Trim();
	}
	#endregion

	#region 公共属性
	public string OptionName { get; }
	public override string Message => this.HasMessage ? base.Message : string.Format(Properties.Resources.InvalidCommandOption_Message, this.OptionName);
	#endregion
}
