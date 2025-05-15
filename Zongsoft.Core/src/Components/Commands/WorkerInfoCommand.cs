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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Commands;

public class WorkerInfoCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly WorkerInfoCommand Default = new WorkerInfoCommand();
	#endregion

	#region 构造函数
	public WorkerInfoCommand() : base("Info") { }
	public WorkerInfoCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		//向上查找工作者命令对象，如果找到则获取其对应的工作者对象
		var worker = context.Find<WorkerCommandBase>(true)?.Worker ?? throw new CommandException("Missing required worker of depends on.");

		//处理工作者信息
		this.Info(context, worker);

		//返回执行成功的工作者
		return ValueTask.FromResult<object>(worker);
	}
	#endregion

	#region 虚拟方法
	protected virtual void Info(CommandContext context, IWorker worker)
	{
		context.Output.WriteLine(GetInfo(worker));
	}
	#endregion

	#region 内部方法
	internal static CommandOutletContent GetInfo(IWorker worker)
	{
		//构建状态内容部分
		var content = CommandOutletContent.Create(Utility.GetStateColor(worker.State), $"[{worker.State}]");

		//构建可用内容部分
		if(!worker.Enabled)
		{
			content.Append(CommandOutletColor.Gray, "(");
			content.Append(CommandOutletColor.Red, Properties.Resources.Disabled);
			content.Append(CommandOutletColor.Gray, ")");
		}

		//构建名称内容部分
		content.Append(" " + worker.Name);

		//获取描述信息注解
		var attribute = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(worker.GetType(), typeof(System.ComponentModel.DescriptionAttribute), true);

		if(attribute != null && !string.IsNullOrWhiteSpace(attribute.Description))
		{
			content.AppendLine();
			content.Append(attribute.Description);
		}

		return content;
	}
	#endregion
}
