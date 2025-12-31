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
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Commands;

[DisplayName("Text.RedisFindCommand.Name")]
[Description("Text.RedisFindCommand.Description")]
[CommandOption(COUNT_OPTION, typeof(int), DefaultValue = 100, Description = "Text.RedisFindCommand.Options.Count")]
public class RedisFindCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string COUNT_OPTION = "count";
	#endregion

	#region 构造函数
	public RedisFindCommand() : base("Find") { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException("Missing arguments.");

		var redis = context.Find<RedisCommand>(true)?.Redis ?? throw new CommandException($"Missing the required redis service.");

		//查找指定模式的键名集
		var result = redis.FindAsync(context.Arguments[0], cancellation);

		//定义遍历序号
		var index = 1;

		var count = context.Options.GetValue<int>(COUNT_OPTION);
		var list = new List<string>(count > 0 ? count : 16);

		await foreach(var key in result)
		{
			if(count > 0 && index > count)
				break;

			list.Add(key);
			context.Output.Write(CommandOutletColor.DarkGray, $"[{index++}] ");
			context.Output.WriteLine(key);
		}

		return list;
	}
	#endregion
}
