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
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("RedisInfoCommand.Name")]
[Description("RedisInfoCommand.Description")]
public class RedisInfoCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisInfoCommand() : base("Info") { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException(Properties.Resources.MissingRequiredRedisService_Message);
		var info = await redis.GetInfoAsync(cancellation);

		var content = CommandOutletContent
			.Create(CommandOutletColor.DarkMagenta, "#" + info.DatabaseId.ToString() + " ")
			.Append(info.Name);

		if(!string.IsNullOrEmpty(info.Namespace))
		{
			content.Append(CommandOutletColor.Gray + "@")
				   .Append(CommandOutletColor.DarkYellow, info.Namespace);
		}

		content.AppendLine().AppendLine(CommandOutletColor.DarkGray, info.Settings.ToString());

		if(info.Servers != null && info.Servers.Length > 0)
		{
			content.AppendLine();

			for(int i = 0; i < info.Servers.Length; i++)
			{
				content.Append(CommandOutletColor.DarkGray, "  [" + (i + 1).ToString() + "] ")
					   .Append(CommandOutletColor.DarkYellow, info.Servers[i].ServerType.ToString())
					   .Append(CommandOutletColor.DarkGreen, " " + info.Servers[i].EndPoint.ToString())
					   .Append(CommandOutletColor.DarkGray, "(")
					   .Append(CommandOutletColor.DarkYellow, Properties.Resources.Version + " " + info.Servers[i].Version.ToString())
					   .Append(CommandOutletColor.DarkGray, ") ")
					   .Append(CommandOutletColor.DarkMagenta, info.Servers[i].IsSlave ? Properties.Resources.Replica : Properties.Resources.Master)
					   .Append(CommandOutletColor.DarkGray, ":");

				if(info.Servers[i].IsConnected)
					content.AppendLine(CommandOutletColor.Green, Properties.Resources.Connected);
				else
					content.AppendLine(CommandOutletColor.DarkRed, Properties.Resources.Disconnected);
			}
		}

		context.Output.WriteLine(content);
		return info;
	}
	#endregion
}
