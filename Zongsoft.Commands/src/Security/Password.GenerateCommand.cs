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

[CommandOption(ALGORITHM_OPTION, 'a', typeof(string), "SHA1")]
[CommandOption(FACTOR_OPTION, 'f', typeof(byte), 10)]
[CommandOption(NONCE_OPTION, 'n', typeof(string))]
public class PasswordGenerateCommand : CommandBase<CommandContext>
{
	private const string ALGORITHM_OPTION = "algorithm";
	private const string FACTOR_OPTION = "factor";
	private const string NONCE_OPTION = "nonce";

	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			return context.Value is string text ?
				ValueTask.FromResult<object>(Generate(text, context.Options)):
				ValueTask.FromResult<object>(null);

		if(context.Arguments.Count == 1)
			return ValueTask.FromResult<object>(Generate(context.Arguments[0], context.Options));

		var result = new Password[context.Arguments.Count];

		for(int i = 0; i < result.Length; i++)
			result[i] = Generate(context.Arguments[i], context.Options);

		return ValueTask.FromResult<object>(result);

		static Password Generate(string value, CommandLine.CmdletOptionCollection options)
		{
			var nonce = ReadOnlySpan<byte>.Empty;

			if(options.TryGetValue<string>(NONCE_OPTION, out var text) && text != null)
			{
				if(int.TryParse(text, out var int32))
					nonce = BitConverter.GetBytes(int32);
				else if(long.TryParse(text, out var int64))
					nonce = BitConverter.GetBytes(int64);
				else if(double.TryParse(text, out var number))
					nonce = BitConverter.GetBytes(number);
				else
					nonce = System.Text.Encoding.UTF8.GetBytes(text);
			}

			return Password.Generate(
				value,
				nonce,
				options.GetValue<int>(FACTOR_OPTION),
				options.GetValue<string>(ALGORITHM_OPTION));
		}
	}
}
