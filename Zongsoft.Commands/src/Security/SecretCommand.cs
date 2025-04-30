﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Security.Commands;

[DisplayName("Text.SecretCommand.Name")]
[Description("Text.SecretCommand.Description")]
public class SecretCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public SecretCommand() : base("Secret") { }
	public SecretCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	/// <summary>获取或设置验证码提供程序。</summary>
	[ServiceDependency]
	public ISecretor Secretor { get; set; }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		switch(context.Parameter)
		{
			case ISecretor secretor:
				this.Secretor = secretor;
				break;
			case IDistributedCache cache:
				this.Secretor = new Secretor(cache, ApplicationContext.Current.Services);
				break;
		}

		return ValueTask.FromResult<object>(this.Secretor);
	}
	#endregion
}
