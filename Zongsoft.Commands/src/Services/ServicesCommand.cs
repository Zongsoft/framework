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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Services.Commands
{
	[DisplayName("Text.ServicesCommand.Name")]
	[Description("Text.ServicesCommand.Description")]
	[CommandOption("provider", typeof(string), Description = "Text.ServicesCommand.Options.Provider")]
	public class ServicesCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		protected ServicesCommand() : base("Services")
		{
		}

		protected ServicesCommand(string name) : base(name)
		{
		}

		public ServicesCommand(IServiceProviderFactory serviceFactory) : this("Services", serviceFactory)
		{
		}

		protected ServicesCommand(string name, IServiceProviderFactory serviceFactory) : base(name)
		{
			this.ServiceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
			this.ServiceProvider = serviceFactory.Default;
		}
		#endregion

		#region 公共属性
		public IServiceProviderFactory ServiceFactory { get; }

		public Zongsoft.Services.IServiceProvider ServiceProvider { get; set; }
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			if(this.ServiceFactory == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "ServiceProviderFactory"));

			string providerName;

			if(context.Expression.Options.TryGetValue("provider", out providerName))
			{
				if(string.IsNullOrWhiteSpace(providerName) || providerName == "~" || providerName == ".")
					ServiceProvider = this.ServiceFactory.Default;
				else
				{
					var provider = this.ServiceFactory.GetProvider(providerName);

					if(provider == null)
						throw new CommandException(string.Format(Properties.Resources.Text_ServicesCommand_NotFoundProvider, providerName));

					ServiceProvider = provider;
				}

				//显示执行成功的信息
				context.Output.WriteLine(Properties.Resources.Text_CommandExecuteSucceed);
			}

			var items = ServiceFactory as IEnumerable<KeyValuePair<string, Zongsoft.Services.IServiceProvider>>;

			if(items != null)
			{
				int index = 1;

				foreach(var item in items)
				{
					context.Output.Write(CommandOutletColor.DarkMagenta, "[{0}] ", index++);

					if(string.IsNullOrWhiteSpace(item.Key))
						context.Output.Write("<Default>");
					else
						context.Output.Write(item.Key);

					if(object.ReferenceEquals(item.Value, this.ServiceProvider))
						context.Output.WriteLine(CommandOutletColor.Green, " (Actived)");
					else
						context.Output.WriteLine();
				}
			}

			return ServiceProvider;
		}
		#endregion

		#region 静态方法
		internal static Zongsoft.Services.IServiceProvider GetServiceProvider(CommandTreeNode node)
		{
			if(node == null)
				return null;

			if(node.Command is ServicesCommand command)
				return command.ServiceProvider;

			return GetServiceProvider(node.Parent);
		}
		#endregion
	}
}
