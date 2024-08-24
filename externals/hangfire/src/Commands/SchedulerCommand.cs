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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire.Commands
{
	public class SchedulerCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private IScheduler _scheduler;
		#endregion

		#region 构造函数
		public SchedulerCommand(IServiceProvider serviceProvider)
		{
			_scheduler = serviceProvider?.Resolve<IScheduler>();
		}

		protected override object OnExecute(CommandContext context)
		{
			return _scheduler;
		}
		#endregion

		#region 静态方法
		public static IScheduler GetScheduler(CommandTreeNode node)
		{
			if(node == null)
				return null;

			if(node.Command is SchedulerCommand command)
				return command._scheduler;

			return GetScheduler(node.Parent) ?? throw new InvalidOperationException("Missing required scheduler.");
		}
		#endregion
	}
}
