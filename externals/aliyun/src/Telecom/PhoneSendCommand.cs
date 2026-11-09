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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Telecom;

[CommandOption(KEY_TEMPLATE_OPTION, 't', typeof(string), null, true, "Text.PhoneSendCommand.Options.Template")]
[CommandOption(KEY_PARAMETERS_OPTION, 'p', typeof(string), null, false, "Text.PhoneSendCommand.Options.Parameters")]
[CommandOption(KEY_SCHEME_OPTION, 's', typeof(string), null, false, "Text.PhoneSendCommand.Options.Scheme")]
[CommandOption(KEY_EXTRA_OPTION, 'e', typeof(string), null, false, "Text.PhoneSendCommand.Options.Extra")]
public class PhoneSendCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string KEY_TEMPLATE_OPTION = "template";
	private const string KEY_PARAMETERS_OPTION = "parameters";
	private const string KEY_SCHEME_OPTION = "scheme";
	private const string KEY_EXTRA_OPTION = "extra";
	#endregion

	#region 成员字段
	private readonly Phone _phone;
	#endregion

	#region 构造函数
	public PhoneSendCommand(Phone phone) : base("Send")
	{
		_phone = phone;
	}
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments == null || context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		var result = await _phone.SendAsync(
			context.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
			context.Arguments,
			context.Value ?? Utility.GetDictionary(context.Options.GetValue<string>(KEY_PARAMETERS_OPTION)),
			context.Options.GetValue<string>(KEY_SCHEME_OPTION),
			context.Options.GetValue<string>(KEY_EXTRA_OPTION), cancellation);

		return result;
	}
	#endregion
}
