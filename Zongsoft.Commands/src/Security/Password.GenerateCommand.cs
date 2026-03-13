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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

[CommandOption(ALGORITHM_OPTION, 'a', typeof(string), "SHA1")]
[CommandOption(EXPONENT_OPTION, 'e', typeof(byte), 10)]
[CommandOption(NONCE_OPTION, 'n', typeof(string))]
public class PasswordGenerateCommand : CommandBase<CommandContext>
{
	private const string ALGORITHM_OPTION = "algorithm";
	private const string EXPONENT_OPTION = "exponent";
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

		for(int i=0; i < result.Length; i++)
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
				options.GetValue<int>(EXPONENT_OPTION),
				options.GetValue<string>(ALGORITHM_OPTION));
		}
	}
}
