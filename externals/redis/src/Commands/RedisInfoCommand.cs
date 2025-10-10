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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("Text.RedisInfoCommand.Name")]
[Description("Text.RedisInfoCommand.Description")]
public class RedisInfoCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public RedisInfoCommand() : base("Info") { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException($"Missing the required redis service.");
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
					   .Append(CommandOutletColor.DarkYellow, "ver " + info.Servers[i].Version.ToString())
					   .Append(CommandOutletColor.DarkGray, ") ")
					   .Append(CommandOutletColor.DarkMagenta, info.Servers[i].IsSlave ? "Slave" : "Master")
					   .Append(CommandOutletColor.DarkGray, ":");

				if(info.Servers[i].IsConnected)
					content.AppendLine(CommandOutletColor.Green, "Connected");
				else
					content.AppendLine(CommandOutletColor.DarkRed, "Unconnected");
			}
		}

		context.Output.WriteLine(content);
		return info;
	}
	#endregion
}
