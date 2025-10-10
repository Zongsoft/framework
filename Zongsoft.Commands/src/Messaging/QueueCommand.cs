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

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Messaging.Commands;

[DisplayName("Text.QueueCommand.Name")]
[Description("Text.QueueCommand.Description")]
[CommandOption("name", typeof(string), Description = "Text.QueueCommand.Options.Name")]
public class QueueCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private readonly IServiceProvider _serviceProvider;
	#endregion

	#region 构造函数
	public QueueCommand(IServiceProvider serviceProvider) : base("Queue")
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public QueueCommand(IServiceProvider serviceProvider, string name) : base(name)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		foreach(var provider in serviceProvider.ResolveAll<IMessageQueueProvider>())
		{
			if(!provider.Exists(name))
				continue;

			var queue = provider.Queue(name);

			if(queue != null)
			{
				this.Queue = queue;
				break;
			}
		}
	}
	#endregion

	#region 公共属性
	public IMessageQueue Queue { get; set; }
	#endregion

	#region 执行方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value is IMessageQueue queue)
			this.Queue = queue;

		if(context.GetOptions().TryGetValue<string>("name", out var name))
		{
			if(string.IsNullOrEmpty(name))
				return ValueTask.FromResult<object>(this.Queue);

			//根据名称获取对应的消息队列
			this.Queue = MessageQueueConverter.Resolve(name) ??
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, name));
		}

		if(this.Queue == null)
			context.Output.WriteLine(CommandOutletColor.Magenta, Properties.Resources.Text_NoQueue);
		else
			context.Output.WriteLine(CommandOutletColor.Green, this.Queue.ToString());

		return ValueTask.FromResult<object>(this.Queue);
	}
	#endregion
}
