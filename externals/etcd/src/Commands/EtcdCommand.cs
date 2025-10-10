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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Etcd library.
 *
 * The Zongsoft.Externals.Etcd is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Etcd is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Etcd library. If not, see <http://www.gnu.org/licenses/>.
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
		var name = context.GetOptions().GetValue<string>("name");

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
