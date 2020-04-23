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

using Microsoft.Extensions.Configuration;

using Zongsoft.Services;

namespace Zongsoft.Configuration.Commands
{
	public class ConfigurationCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private IConfiguration _configuration;
		#endregion

		#region 构造函数
		public ConfigurationCommand() : base("Options")
		{
			_configuration = Zongsoft.Services.ApplicationContext.Current.Configuration;
		}

		public ConfigurationCommand(string name) : base(name)
		{
			_configuration = Zongsoft.Services.ApplicationContext.Current.Configuration;
		}
		#endregion

		#region 公共属性
		public IConfiguration Configuration
		{
			get => _configuration;
			set => _configuration = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Parameter is IConfiguration configuration)
				_configuration = configuration;

			//打印配置信息
			Print(_configuration, context.Output, 0);

			return _configuration;
		}
		#endregion

		#region 静态方法
		internal static IConfiguration GetConfiguration(CommandTreeNode node)
		{
			if(node == null)
				return null;

			if(node.Command is ConfigurationCommand command)
				return command.Configuration;

			return GetConfiguration(node.Parent);
		}

		internal static void Print(IConfiguration configuration, ICommandOutlet output, int depth)
		{
			if(configuration == null)
				return;

			if(configuration is IConfigurationRoot root)
			{
				int index = 0;

				foreach(var provider in root.Providers)
				{
					output.WriteLine(CommandOutletContent
						.Create(CommandOutletColor.Gray, $"[{++index}] ")
						.Append(CommandOutletColor.DarkGreen, provider.GetType().FullName)
					);
				}

				output.WriteLine();
				output.WriteLine(root.GetDebugView());
			}
			else if(configuration is IConfigurationSection section)
			{
				if(depth > 0)
					output.Write(new string('\t', depth));

				if(section.Value == null)
				{
					output.WriteLine(CommandOutletColor.DarkYellow, section.Path);
				}
				else
				{
					output.WriteLine(CommandOutletContent
						.Create(CommandOutletColor.DarkGreen, ConfigurationPath.GetParentPath(section.Path))
						.Append(CommandOutletColor.DarkCyan, ":")
						.Append(CommandOutletColor.Green, section.Key)
						.Append(CommandOutletColor.DarkMagenta, "=")
						.Append(CommandOutletColor.DarkGray, section.Value)
					);
				}

				foreach(var child in section.GetChildren())
				{
					Print(child, output, depth + 1);
				}
			}
		}
		#endregion
	}
}
