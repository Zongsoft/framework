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

namespace Zongsoft.Components.Commands;

public abstract class WorkerCommandBase : CommandBase<CommandContext>
{
	#region 成员字段
	private IWorker _worker;
	#endregion

	#region 构造函数
	protected WorkerCommandBase(string name, IServiceProvider serviceProvider = null) : base(name)
	{
		this.ServiceProvider = serviceProvider;
	}

	protected WorkerCommandBase(string name, bool enabled, IServiceProvider serviceProvider = null) : base(name, enabled)
	{
		this.ServiceProvider = serviceProvider;
	}
	#endregion

	#region 保护属性
	internal protected IWorker Worker
	{
		get => _worker;
		set
		{
			if(value == null)
				throw new ArgumentNullException();

			//如果引用相等则不用处理
			if(object.ReferenceEquals(value, _worker))
				return;

			//更新新的工作器
			_worker = value;

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Worker));
		}
	}

	protected IServiceProvider ServiceProvider { get; set; }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		//如果传入的参数对象是一个工作者，则将其设为关联者
		if(context.Value is IWorker worker)
			this.Worker = worker;

		//始终返回关联的工作者对象
		return ValueTask.FromResult<object>(_worker);
	}
	#endregion
}
