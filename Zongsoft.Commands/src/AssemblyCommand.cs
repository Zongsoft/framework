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
using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

using Zongsoft.Services;

namespace Zongsoft.Commands
{
	[DisplayName("Text.AssemblyCommand.Name")]
	[Description("Text.AssemblyCommand.Description")]
	[CommandOption("sort", Type = typeof(SortMode), DefaultValue=SortMode.None, Description = "Text.SortMode")]
	public class AssemblyCommand : Zongsoft.Services.CommandBase<CommandContext>
	{
		#region 成员变量
		private Assembly[] _assemblies;
		#endregion

		#region 构造函数
		public AssemblyCommand() : base("Assembly")
		{
		}

		public AssemblyCommand(string name) : base(name)
		{
		}
		#endregion

		#region 公共属性
		public Assembly[] Assemblies
		{
			get
			{
				if(_assemblies == null || _assemblies.Length < 1)
					_assemblies = this.GetAssemblies();

				return _assemblies;
			}
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			//设置遍历的程序集列表
			Assembly[] assemblies = this.Assemblies;

			switch(context.Expression.Options.GetValue<SortMode>("sort"))
			{
				case SortMode.Asc:
					assemblies = this.Assemblies.OrderBy(p => p.FullName).ToArray();
					break;
				case SortMode.Desc:
					assemblies = this.Assemblies.OrderByDescending(p => p.FullName).ToArray();
					break;
			}

			for(int i = 0; i < assemblies.Length; i++)
			{
				//显示程序集序号
				context.Output.Write(CommandOutletColor.DarkGray, "[");
				context.Output.Write(CommandOutletColor.Magenta, i + 1);
				context.Output.Write(CommandOutletColor.DarkGray, "] ");

				//显示当前程序集的详细信息
				this.PrintAssemblyInfo(context, assemblies[i]);

				if(i < this.Assemblies.Length - 1)
					context.Output.WriteLine();
			}

			return assemblies;
		}
		#endregion

		#region 虚拟方法
		protected virtual Assembly[] GetAssemblies()
		{
			return AppDomain.CurrentDomain.GetAssemblies();
		}

		protected virtual void PrintAssemblyInfo(CommandContext context, Assembly assembly)
		{
			if(assembly == null)
				return;

			context.Output.WriteLine("{0}", assembly.FullName);
			context.Output.Write(CommandOutletColor.DarkYellow, "{0}", assembly.Location);
			context.Output.WriteLine(CommandOutletColor.DarkGray, " [{0}]", File.GetLastWriteTime(assembly.Location));
		}
		#endregion

		#region 枚举定义
		public enum SortMode
		{
			[Description("Text.SortMode.None")]
			None,

			[Description("Text.SortMode.Asc")]
			Asc,

			[Description("Text.SortMode.Desc")]
			Desc,
		}
		#endregion
	}
}
