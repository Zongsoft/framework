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

[CommandOption(KEY_TEMPLATE_OPTION, 't', typeof(string), null, true, "Text.PhoneCallCommand.Options.Template")]
[CommandOption(KEY_PARAMETERS_OPTION, 'p', typeof(string), null, false, "Text.PhoneCallCommand.Options.Parameters")]
[CommandOption(KEY_EXTRA_OPTION, 'e', typeof(string), null, false, "Text.PhoneCallCommand.Options.Extra")]
[CommandOption(KEY_INTERACTIVE_OPTION, 'i', Description = "Text.PhoneCallCommand.Options.Interactive")]
public class PhoneCallCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string KEY_TEMPLATE_OPTION = "template";
	private const string KEY_PARAMETERS_OPTION = "parameters";
	private const string KEY_EXTRA_OPTION = "extra";
	private const string KEY_INTERACTIVE_OPTION = "interactive";
	#endregion

	#region 成员字段
	private readonly Phone _phone;
	#endregion

	#region 构造函数
	public PhoneCallCommand(Phone phone) : base("Call")
	{
		_phone = phone;
	}
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments == null || context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		var result = await this.CallAsync(
			context.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
			context.Arguments,
			context.Value ?? Utility.GetDictionary(context.Options.GetValue<string>(KEY_PARAMETERS_OPTION)),
			context.Options.GetValue<string>(KEY_EXTRA_OPTION),
			context.Options.Contains(KEY_INTERACTIVE_OPTION),
			cancellation);

		return result;
	}

	private async Task<Phone.Result[]> CallAsync(string templateCode, string[] phoneNumbers, object parameter, string extra, bool interactive, CancellationToken cancellation)
	{
		var results = new Phone.Result[phoneNumbers.Length];

		for(int i = 0; i < phoneNumbers.Length; i++)
		{
			if(interactive)
				results[i] = await _phone.CallAsync(templateCode, phoneNumbers[i], string.IsNullOrEmpty(extra) ? null : new Phone.InteractionArgument(extra), cancellation);
			else
				results[i] = await _phone.CallAsync(templateCode, phoneNumbers[i], parameter, extra, cancellation);
		}

		return results;
	}
	#endregion
}
