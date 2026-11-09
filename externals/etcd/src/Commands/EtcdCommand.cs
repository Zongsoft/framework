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

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Etcd.Commands;

[CommandOption("name", 'n')]
public class EtcdCommand : CommandBase<CommandContext>, IServiceAccessor<EtcdService>
{
	#region 成员字段
	private EtcdService _etcd;
	#endregion

	#region 构造函数
	public EtcdCommand() : base("Etcd") { }
	#endregion

	#region 公共属性
	public EtcdService Etcd
	{
		get => _etcd;
		set => _etcd = value ?? throw new ArgumentNullException();
	}

	EtcdService IServiceAccessor<EtcdService>.Value => this.Etcd;
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var name = context.Options.GetValue<string>("name");

		if(!string.IsNullOrEmpty(name))
			_etcd = EtcdServiceProvider.GetEtcd(name) ?? throw new CommandException();

		if(_etcd == null)
			context.Output.WriteLine(CommandOutletColor.Magenta, "");
		else
			context.Output.WriteLine(CommandOutletColor.Green, _etcd.Settings);

		return ValueTask.FromResult<object>(_etcd);
	}
	#endregion
}

public abstract class EtcdCommandBase(string name) : CommandBase<CommandContext>(name)
{
	protected static EtcdService GetEtcd(CommandContext context) =>
		context.Find<EtcdCommand>(true)?.Etcd ?? throw new CommandException(Properties.Resources.Etcd_ServiceRequired_Message);

	protected static void RequireArguments(CommandContext context)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);
	}
}
