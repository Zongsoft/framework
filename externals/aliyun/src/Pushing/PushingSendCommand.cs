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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Pushing;

[CommandOption("Name", typeof(string), null, "Text.NotificationSendCommand.Options.Name")]
[CommandOption("Title", typeof(string), null, "Text.NotificationSendCommand.Options.Title")]
[CommandOption("Expiry", typeof(int), -1, "Text.NotificationSendCommand.Options.Expiry")]
[CommandOption("Type", typeof(PushingType), PushingType.Message, "Text.NotificationSendCommand.Options.Type")]
[CommandOption("DeviceType", typeof(PushingDeviceType), PushingDeviceType.Android, "Text.NotificationSendCommand.Options.DeviceType")]
[CommandOption("TargetType", typeof(PushingTargetType), PushingTargetType.Alias, "Text.NotificationSendCommand.Options.TargetType")]
[CommandOption("Target", typeof(string), null, true, "Text.NotificationSendCommand.Options.Target")]
public class PushingSendCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private PushingSender _sender;
	#endregion

	#region 构造函数
	public PushingSendCommand() : base("Send") { }
	public PushingSendCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	[ServiceDependency(IsRequired = true)]
	public PushingSender Sender
	{
		get => _sender;
		set => _sender = value;
	}
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value == null && context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		var destination = context.Options.GetValue<string>("target");

		if(string.IsNullOrWhiteSpace(destination))
			throw new CommandOptionMissingException("target");

		var settings = new PushingSenderSettings(context.Options.GetValue<PushingType>("type"),
													  context.Options.GetValue<PushingDeviceType>("deviceType"),
													  context.Options.GetValue<PushingTargetType>("targetType"),
													  context.Options.GetValue<int>("expiry"));

		var results = new List<PushingResult>();

		if(context.Value != null)
		{
			var content = await GetContentAsync(context.Value, cancellation);

			var result = await this.SendAsync(
				context.Options.GetValue<string>("name"),
				context.Options.GetValue<string>("title"),
				content, destination, settings, _ => context.Error.WriteLine(Properties.Resources.Text_NotificationSendCommand_Faild),
				cancellation);

			if(result != null)
				results.Add(result);
		}

		foreach(var argument in context.Arguments)
		{
			var result = await this.SendAsync(
				context.Options.GetValue<string>("name"),
				context.Options.GetValue<string>("title"),
				argument, destination, settings, _ => context.Error.WriteLine(Properties.Resources.Text_NotificationSendCommand_Faild),
				cancellation);

			if(result != null)
				results.Add(result);
		}

		if(results.Count == 0)
			return null;
		else if(results.Count == 1)
			return results[0];

		return results;
	}
	#endregion

	#region 私有方法
	private async ValueTask<PushingResult> SendAsync(string name, string title, string content, string destination, PushingSenderSettings settings, Action<PushingResult> onFaild, CancellationToken cancellation)
	{
		var result = await _sender.SendAsync(name, title, content, destination, settings, cancellation);

		if(result != null)
		{
			if(!result.IsSucceed && onFaild != null)
				onFaild(result);
		}

		return result;
	}

	private static ValueTask<string> GetContentAsync(object value, CancellationToken cancellation)
	{
		if(value == null)
			return ValueTask.FromResult<string>(null);

		if(value is string text)
			return ValueTask.FromResult(text);

		if(value is System.Text.StringBuilder || Zongsoft.Common.TypeExtension.IsScalarType(value.GetType()))
			return ValueTask.FromResult(value.ToString());

		return Zongsoft.Serialization.Serializer.Json.SerializeAsync(value, cancellation);
	}
	#endregion
}
