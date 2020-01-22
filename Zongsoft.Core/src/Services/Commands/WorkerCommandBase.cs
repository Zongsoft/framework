/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

namespace Zongsoft.Services.Commands
{
	public abstract class WorkerCommandBase : CommandBase<CommandContext>
	{
		#region 成员字段
		private IWorker _worker;
		#endregion

		#region 构造函数
		protected WorkerCommandBase(string name) : base(name)
		{
		}

		protected WorkerCommandBase(string name, bool enabled) : base(name, enabled)
		{
		}
		#endregion

		#region 保护属性
		internal protected IWorker Worker
		{
			get
			{
				return _worker;
			}
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
				this.OnPropertyChanged(nameof(Worker));
			}
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			//如果传入的参数对象是一个工作者，则将其设为关联者
			if(context.Parameter is IWorker worker)
				this.Worker = worker;

			//始终返回关联的工作者对象
			return _worker;
		}
		#endregion

		#region 静态方法
		public static CommandOutletColor GetStateColor(WorkerState state)
		{
			switch(state)
			{
				case WorkerState.Pausing:
				case WorkerState.Paused:
					return CommandOutletColor.Yellow;
				case WorkerState.Resuming:
				case WorkerState.Starting:
					return CommandOutletColor.DarkCyan;
				case WorkerState.Stopped:
				case WorkerState.Stopping:
					return CommandOutletColor.DarkGray;
				default:
					return CommandOutletColor.Green;
			}
		}
		#endregion
	}
}
