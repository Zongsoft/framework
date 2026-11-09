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
using System.Security.Cryptography;

using Zongsoft.Components;

namespace Zongsoft.Security.Commands;

[CommandOption(SIZE_OPTION, typeof(int))]
public partial class RSACommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string SIZE_OPTION = "size";
	#endregion

	#region 构造函数
	public RSACommand() : base("RSA") { }
	public RSACommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public RSA RSA { get; set; }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var rsa = this.RSA;

		if(rsa == null)
		{
			rsa = this.RSA = context.Options.TryGetValue<int>(SIZE_OPTION, out var size) && size > 0 ? RSA.Create(size) : RSA.Create();
		}
		else
		{
			if(context.Options.TryGetValue<int>(SIZE_OPTION, out var size) && size > 0)
				rsa = this.RSA = RSA.Create(size);
		}

		context.Output.WriteLine(CommandOutletColor.DarkMagenta, rsa.KeySize);

		var parameters = rsa.ExportParameters(true);
		var content = CommandOutletContent.Create(CommandOutletColor.DarkYellow, nameof(RSAParameters.Modulus))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.Modulus))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.Exponent))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.Exponent))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.D))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.D))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.P))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.P))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.Q))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.Q))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.DP))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.DP))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.DQ))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.DQ))

			.Append(CommandOutletColor.DarkYellow, nameof(RSAParameters.InverseQ))
			.Append(CommandOutletColor.DarkGray, "=")
			.AppendLine(CommandOutletColor.DarkGreen, Convert.ToBase64String(parameters.InverseQ));

		context.Output.Write(content);

		return ValueTask.FromResult<object>(rsa);
	}
	#endregion
}
