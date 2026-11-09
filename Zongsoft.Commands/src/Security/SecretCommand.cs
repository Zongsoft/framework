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

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Security.Commands;

[DisplayName("SecretCommand.Name")]
[Description("SecretCommand.Description")]
public partial class SecretCommand : CommandBase<CommandContext>
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
		switch(context.Value)
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
