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

public class PasswordParseCommand : CommandBase<CommandContext>
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			return context.Value switch
			{
				string text => ValueTask.FromResult<object>(Parse(text)),
				byte[] data => ValueTask.FromResult<object>(Parse(data)),
				_ => ValueTask.FromResult<object>(null),
			};

		if(context.Arguments.Count == 1)
			return ValueTask.FromResult<object>(Parse(context.Arguments[0]));

		var result = new Password[context.Arguments.Count];

		for(int i = 0; i < result.Length; i++)
			result[i] = Parse(context.Arguments[i]);

		return ValueTask.FromResult<object>(result);
	}

	static Password Parse(ReadOnlySpan<char> text) => Password.TryParse(text, out var password) ? password : throw new InvalidOperationException(string.Format(Properties.Resources.Password_TextInvalid_Message, text.ToString()));
	static Password Parse(ReadOnlySpan<byte> data) => Password.TryParse(data, out var password) ? password : throw new InvalidOperationException(Properties.Resources.Password_DataInvalid_Message);
}
