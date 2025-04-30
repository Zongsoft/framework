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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Security.Commands;

[CommandOption(TYPE_OPTION, typeof(RSAKeyType))]
[CommandOption(FORMAT_OPTION, typeof(RSAKeyFormat))]
public class RSAExportCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string TYPE_OPTION = "type";
	private const string FORMAT_OPTION = "format";
	#endregion

	#region 构造函数
	public RSAExportCommand() : base("Export") { }
	public RSAExportCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var rsa = (context.CommandNode.Find<RSACommand>(true)?.RSA) ?? throw new CommandException("Missing the required RSA.");

		object result = context.Expression.Options.GetValue<RSAKeyType>(TYPE_OPTION) switch
		{
			RSAKeyType.All => rsa.ToXmlString(true),
			RSAKeyType.Public => rsa.ExportRSAPublicKey(),
			RSAKeyType.Private => context.Expression.Options.GetValue<RSAKeyFormat>(FORMAT_OPTION) == RSAKeyFormat.Pkcs8 ?
				rsa.ExportPkcs8PrivateKey() :
				rsa.ExportRSAPrivateKey(),
			RSAKeyType.Subject => rsa.ExportSubjectPublicKeyInfo(),
			_ => rsa.ExportSubjectPublicKeyInfo(),
		};

		return ValueTask.FromResult(result);
	}
	#endregion
}
