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

using Zongsoft.Components;

namespace Zongsoft.Security.Commands;

partial class RSACommand
{
	[CommandOption(TYPE_OPTION, typeof(RSAKeyType))]
	[CommandOption(FORMAT_OPTION, typeof(RSAKeyFormat))]
	public class ExportCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string TYPE_OPTION = "type";
		private const string FORMAT_OPTION = "format";
		#endregion

		#region 构造函数
		public ExportCommand() : base("Export") { }
		public ExportCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			var rsa = (context.Find<RSACommand>(true)?.RSA) ?? throw new CommandException(Properties.Resources.Security_RsaRequired_Message);

			object result = context.Options.GetValue<RSAKeyType>(TYPE_OPTION) switch
			{
				RSAKeyType.All => rsa.ToXmlString(true),
				RSAKeyType.Public => rsa.ExportRSAPublicKey(),
				RSAKeyType.Private => context.Options.GetValue<RSAKeyFormat>(FORMAT_OPTION) == RSAKeyFormat.Pkcs8 ?
					rsa.ExportPkcs8PrivateKey() :
					rsa.ExportRSAPrivateKey(),
				RSAKeyType.Subject => rsa.ExportSubjectPublicKeyInfo(),
				_ => rsa.ExportSubjectPublicKeyInfo(),
			};

			return ValueTask.FromResult(result);
		}
		#endregion
	}
}