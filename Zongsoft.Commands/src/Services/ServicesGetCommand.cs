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

namespace Zongsoft.Services.Commands
{
	/// <summary>
	/// 该命令名为“get”，本命令提供获取当前服务供应程序中的各种服务对象，并显式其信息。
	/// </summary>
	/// <remarks>
	///		<para>该命令的用法如下：</para>
	///		<list type="table">
	///			<item>
	///				<term>[services.]get name1 name2 ... name#n</term>
	///				<description>参数为要获取的服务名称，该名称为其在当前服务供应程序注册时声明的名称。</description>
	///			</item>
	///			<item>
	///				<term>[services.]get -contract:assemblyQualifiedName</term>
	///				<description>选项为要获取的服务类型限定名。</description>
	///			</item>
	///			<item>
	///				<term>[services.]get -contract:assemblyQualifiedName parameter</term>
	///				<description>选项为要获取的服务类型限定名，参数为解析时的选择参数。</description>
	///			</item>
	///		</list>
	/// </remarks>
	[DisplayName("Text.ServicesGetCommand.Name")]
	[Description("Text.ServicesGetCommand.Description")]
	[CommandOption("contract", Type = typeof(string))]
	public class ServicesGetCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public ServicesGetCommand() : base("Get")
		{
		}

		public ServicesGetCommand(string name) : base(name)
		{
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			var serviceProvider = ServicesCommand.GetServiceProvider(context.CommandNode);

			if(serviceProvider == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "ServiceProvider"));

			var contractText = context.Expression.Options.GetValue<string>("contract");
			Type contract = null;

			if(!string.IsNullOrWhiteSpace(contractText))
			{
				contract = Type.GetType(contractText, false);

				if(contract == null)
					throw new CommandOptionValueException("contract", contractText);
			}

			object result;

			if(contract == null)
			{
				if(context.Expression.Arguments.Length == 0)
					throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

				if(context.Expression.Arguments.Length == 1)
					result = serviceProvider.ResolveRequired(context.Expression.Arguments[0]);
				else
				{
					result = new object[context.Expression.Arguments.Length];

					for(int i = 0; i < context.Expression.Arguments.Length; i++)
						((object[])result)[i] = serviceProvider.ResolveRequired(context.Expression.Arguments[i]);
				}
			}
			else
			{
				object parameter = null;

				if(context.Expression.Arguments.Length > 1)
					parameter = context.Expression.Arguments;
				else if(context.Expression.Arguments.Length == 1)
					parameter = context.Expression.Arguments[0];

				result = serviceProvider.ResolveRequired(contract, parameter);
			}

			//打印获取的结果信息
			context.Output.WriteLine(Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(result));

			return result;
		}
		#endregion
	}
}
