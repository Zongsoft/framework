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

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Messaging.Commands;

[DisplayName("QueueCommand.Name")]
[Description("QueueCommand.Description")]
[CommandOption("name", typeof(string), Description = "QueueCommand.Options.Name")]
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

		if(context.Options.TryGetValue<string>("name", out var name))
		{
			if(string.IsNullOrEmpty(name))
				return ValueTask.FromResult<object>(this.Queue);

			//根据名称获取对应的消息队列
			this.Queue = MessageQueueConverter.Resolve(name) ??
				throw new CommandException(string.Format(Properties.Resources.CannotObtainCommandTarget, name));
		}

		if(this.Queue == null)
			context.Output.WriteLine(CommandOutletColor.Magenta, Properties.Resources.NoQueue);
		else
			context.Output.WriteLine(CommandOutletColor.Green, this.Queue.ToString());

		return ValueTask.FromResult<object>(this.Queue);
	}
	#endregion
}
