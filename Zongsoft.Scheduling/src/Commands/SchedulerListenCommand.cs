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
 * This file is part of Zongsoft.Scheduling library.
 *
 * The Zongsoft.Scheduling is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Scheduling is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Scheduling library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Scheduling.Commands
{
	public class SchedulerListenCommand : Zongsoft.Services.Commands.WorkerListenCommandBase<Scheduler>
	{
		#region 构造函数
		public SchedulerListenCommand() { }
		public SchedulerListenCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override void OnListening(CommandContext context, Scheduler scheduler)
		{
			scheduler.Expired += this.Scheduler_Expired;
			scheduler.Handled += this.Scheduler_Handled;
			scheduler.Occurred += this.Scheduler_Occurred;
			scheduler.Occurring += this.Scheduler_Occurring;
			scheduler.Scheduled += this.Scheduler_Scheduled;

			if(scheduler.Retriever != null)
			{
				scheduler.Retriever.Failed += this.Retriever_Failed;
				scheduler.Retriever.Succeed += this.Retriever_Succeed;
				scheduler.Retriever.Discarded += this.Retriever_Discarded;
			}

			//调用基类同名方法（打印欢迎信息）
			base.OnListening(context, scheduler);

			//打印基本信息
			context.Output.WriteLine(SchedulerCommand.GetInfo(scheduler, true).AppendLine());
		}

		protected override void OnListened(CommandContext context, Scheduler scheduler)
		{
			scheduler.Expired -= this.Scheduler_Expired;
			scheduler.Handled -= this.Scheduler_Handled;
			scheduler.Occurred -= this.Scheduler_Occurred;
			scheduler.Occurring -= this.Scheduler_Occurring;
			scheduler.Scheduled -= this.Scheduler_Scheduled;

			if(scheduler.Retriever != null)
			{
				scheduler.Retriever.Failed -= this.Retriever_Failed;
				scheduler.Retriever.Succeed -= this.Retriever_Succeed;
				scheduler.Retriever.Discarded -= this.Retriever_Discarded;
			}
		}
		#endregion

		#region 事件处理
		private void Scheduler_Expired(object sender, ExpiredEventArgs e)
		{
		}

		private void Scheduler_Handled(object sender, HandledEventArgs e)
		{
			//根据处理完成事件参数来设置标志名
			var name = e.Exception == null ? Properties.Resources.Scheduler_Handled_Succeed : Properties.Resources.Scheduler_Handled_Failed;

			//获取处理完成的事件信息内容
			var content = GetHandledContent(name, CommandOutletColor.DarkGreen, e);

			//输出事件信息内容
			this.Context.Output.WriteLine(content);
		}

		private void Scheduler_Occurred(object sender, OccurredEventArgs e)
		{
			//获取调度器的基本信息内容（不需包含状态信息）
			var content = SchedulerCommand.GetInfo((IScheduler)sender, false);

			content.Prepend(Properties.Resources.Scheduler_Occurred_Name)
				.After(CommandOutletColor.DarkGray, "(")
				.After(CommandOutletColor.DarkCyan, e.EventId)
				.After(CommandOutletColor.DarkGray, "): ")
				.After(CommandOutletColor.Magenta, e.Count.ToString() + " ");

			this.Context.Output.WriteLine(content.First);
		}

		private void Scheduler_Occurring(object sender, OccurringEventArgs e)
		{
			//获取调度器的基本信息内容（不需包含状态信息）
			var content = SchedulerCommand.GetInfo((IScheduler)sender, false);

			content.Prepend(Properties.Resources.Scheduler_Occurring_Name)
				.After(CommandOutletColor.DarkGray, "(")
				.After(CommandOutletColor.DarkCyan, e.EventId)
				.After(CommandOutletColor.DarkGray, ")");

			this.Context.Output.WriteLine(content.First);
		}

		private void Scheduler_Scheduled(object sender, ScheduledEventArgs e)
		{
			//获取调度器的基本信息内容（不需包含状态信息）
			var content = SchedulerCommand.GetInfo((IScheduler)sender, false);

			content.Prepend(Properties.Resources.Scheduler_Scheduled_Name)
				.After(CommandOutletColor.DarkGray, "(")
				.After(CommandOutletColor.DarkCyan, e.EventId)
				.After(CommandOutletColor.DarkGray, "): ")
				.After(CommandOutletColor.Magenta, e.Count.ToString() + " ");

			if(e.Triggers != null && e.Triggers.Length > 0)
			{
				content.AppendLine();

				for(int i = 0; i < e.Triggers.Length; i++)
				{
					content.Append(CommandOutletColor.DarkYellow, $"[{i + 1}] ")
					       .AppendLine(e.Triggers[i].ToString());
				}
			}

			this.Context.Output.WriteLine(content.First);
		}

		private void Retriever_Failed(object sender, HandledEventArgs e)
		{
			//获取重试失败的事件信息内容
			var content = GetHandledContent(Properties.Resources.Retriever_Failed_Name, CommandOutletColor.DarkRed, e);

			//输出事件信息内容
			this.Context.Output.WriteLine(content);
		}

		private void Retriever_Succeed(object sender, HandledEventArgs e)
		{
			//获取重试成功的事件信息内容
			var content = GetHandledContent(Properties.Resources.Retriever_Succeed_Name, CommandOutletColor.DarkYellow, e);

			//输出事件信息内容
			this.Context.Output.WriteLine(content);
		}

		private void Retriever_Discarded(object sender, HandledEventArgs e)
		{
			//获取重试丢弃的事件信息内容
			var content = GetHandledContent(Properties.Resources.Retriever_Discarded_Name, CommandOutletColor.Magenta, e);

			//输出事件信息内容
			this.Context.Output.WriteLine(content);
		}
		#endregion

		#region 私有方法
		private static CommandOutletContent GetHandledContent(string name, CommandOutletColor color, HandledEventArgs args)
		{
			var content = CommandOutletContent.Create(color, name)
				.Append(CommandOutletColor.DarkGray, "(")
				.Append(CommandOutletColor.DarkCyan, args.Context.EventId)
				.Append(CommandOutletColor.DarkGray, ".")
				.Append(CommandOutletColor.DarkBlue, (args.Context.Index + 1).ToString())
				.Append(CommandOutletColor.DarkGray, "): ")
				.Append(CommandOutletColor.DarkYellow, $"[{args.Context.ScheduleId}] ")
				.Append(CommandOutletColor.DarkCyan, args.Handler.ToString())
				.Append(CommandOutletColor.DarkGray, "@")
				.Append(CommandOutletColor.DarkMagenta, args.Context.Trigger.ToString());

			if(!string.IsNullOrWhiteSpace(args.Context.Trigger.Description))
				content.Append(CommandOutletColor.DarkGray, "(" + args.Context.Trigger.Description + ")");

			if(args.Context.Failure.HasValue)
			{
				var failure = args.Context.Failure.Value;

				//为重试信息添加起始标记
				content.Append(CommandOutletColor.Gray, " {");

				if(failure.Count > 0 && failure.Timestamp.HasValue)
				{
					content.Append(CommandOutletColor.DarkYellow, "#");
					content.Append(CommandOutletColor.DarkRed, failure.Count.ToString());
					content.Append(CommandOutletColor.DarkYellow, "#");

					content.Append(CommandOutletColor.DarkGray, " (");
					content.Append(CommandOutletColor.DarkRed, failure.Timestamp.HasValue ? failure.Timestamp.ToString() : Properties.Resources.Scheduler_Retry_NoTimestamp);
					content.Append(CommandOutletColor.DarkGray, ")");
				}

				if(failure.Expiration.HasValue)
				{
					content.Append(CommandOutletColor.Red, " < ");
					content.Append(CommandOutletColor.DarkGray, "(");
					content.Append(CommandOutletColor.DarkMagenta, failure.Expiration.HasValue ? failure.Expiration.ToString() : Properties.Resources.Scheduler_Retry_NoExpiration);
					content.Append(CommandOutletColor.DarkGray, ")");
				}

				//为重试信息添加结束标记
				content.Append(CommandOutletColor.Gray, "}");
			}

			if(args.Exception != null)
			{
				//设置名称内容端的文本颜色为红色
				content.First.Color = CommandOutletColor.Red;

				content.AppendLine()
				       .Append(GetExceptionContent(args.Exception));
			}

			return content;
		}

		private static CommandOutletContent GetExceptionContent(Exception exception)
		{
			if(exception == null)
				return null;

			//哔哔一下
			Console.Beep();

			//为异常信息添加起始标记
			var content = CommandOutletContent.Create(CommandOutletColor.Gray, "{" + Environment.NewLine);

			while(exception != null)
			{
				content.Append(CommandOutletColor.Red, "    " + exception.GetType().FullName);

				if(!string.IsNullOrEmpty(exception.Source))
				{
					content.Append(CommandOutletColor.DarkGray, "(")
					       .Append(CommandOutletColor.DarkYellow, exception.Source)
					       .Append(CommandOutletColor.DarkGray, ")");
				}

				content.Append(CommandOutletColor.DarkGray, ": ")
				       .AppendLine(exception.Message.Replace('\r', ' ').Replace('\n', ' '));

				exception = exception.InnerException;
			}

			//为异常信息添加结束标记
			return content.Append(CommandOutletColor.Gray, "}");
		}
		#endregion
	}
}
