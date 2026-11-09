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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Commands;

[CommandOption(ALGORITHM_OPTION, 'a', typeof(string))]
[CommandOption(ENCODING_OPTION, 'e', typeof(Encoding))]
public class ChecksumCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string ALGORITHM_OPTION = "algorithm";
	private const string ENCODING_OPTION = "encoding";
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var algorithm = context.Options.GetValue<string>(ALGORITHM_OPTION);
		var encoding = context.Options.GetValue<Encoding>(ENCODING_OPTION) ?? Encoding.UTF8;

		if(context.Arguments.IsEmpty)
			return context.Value switch
			{
				null => null,
				byte[] buffer => Checksum.Compute(algorithm, buffer),
				string text => Checksum.Compute(algorithm, encoding.GetBytes(text)),
				System.IO.Stream stream => await Checksum.ComputeAsync(algorithm, stream, cancellation),
				_ => throw new InvalidOperationException(string.Format(Properties.Resources.Checksum_TypeUnsupported_Message, context.Value.GetType().GetAlias())),
			};

		if(context.Arguments.Count == 1)
			return Checksum.Compute(algorithm, encoding.GetBytes(context.Arguments[0]));

		var result = new Checksum[context.Arguments.Count];
		for(int i = 0; i < result.Length; i++)
			result[i] = Checksum.Compute(algorithm, encoding.GetBytes(context.Arguments[i]));
		return result;
	}
	#endregion
}
