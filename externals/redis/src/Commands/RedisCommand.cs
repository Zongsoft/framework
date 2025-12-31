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

[DisplayName("Text.RedisCommand.Name")]
[Description("Text.RedisCommand.Description")]
[CommandOption("name", 'n')]
public class RedisCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private RedisService _redis;
	#endregion

	#region 构造函数
	public RedisCommand() : base("Redis") { }
	#endregion

	#region 公共属性
	public RedisService Redis
	{
		get => _redis;
		set => _redis = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var name = context.Options.GetValue<string>("name");

		if(!string.IsNullOrEmpty(name))
			_redis = RedisServiceProvider.GetRedis(name) ?? throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, name));

		if(_redis == null)
			context.Output.WriteLine(CommandOutletColor.Magenta, Properties.Resources.Text_NoRedis);
		else
			context.Output.WriteLine(CommandOutletColor.Green, _redis.Settings);

		return ValueTask.FromResult<object>(_redis);
	}
	#endregion
}
