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

	static Password Parse(ReadOnlySpan<char> text) => Password.TryParse(text, out var password) ? password : throw new InvalidOperationException($"");
	static Password Parse(ReadOnlySpan<byte> data) => Password.TryParse(data, out var password) ? password : throw new InvalidOperationException($"");
}
