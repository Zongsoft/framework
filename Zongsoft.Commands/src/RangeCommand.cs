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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Commands;

[CommandOption(TYPE_OPTION, 't', typeof(string), "int")]
[CommandOption(MINIMUM_OPTION, typeof(int), 0)]
[CommandOption(MAXIMUM_OPTION, typeof(int), 9)]
[CommandOption(ORDERED_OPTION, 'o', typeof(bool), true)]
public class RangeCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string TYPE_OPTION = "type";
	private const string MINIMUM_OPTION = "min";
	private const string MAXIMUM_OPTION = "max";
	private const string ORDERED_OPTION = "ordered";
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(!TypeAlias.TryParse(context.Options.GetValue<string>(TYPE_OPTION), out var type))
			throw new CommandOptionException(TYPE_OPTION);

		if(!context.Options.TryGetValue<int>(MINIMUM_OPTION, out var minimum))
			throw new CommandOptionException(MINIMUM_OPTION);

		if(!context.Options.TryGetValue<int>(MAXIMUM_OPTION, out var maximum))
			throw new CommandOptionException(MAXIMUM_OPTION);

		if(minimum > maximum)
			throw new CommandOptionException($"{MINIMUM_OPTION},{MAXIMUM_OPTION}", $"The value of the '{MINIMUM_OPTION}' option cannot be greater than the value of the '{MAXIMUM_OPTION}' option.");

		var count = maximum - minimum + 1;

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Byte:
				var bytes = new byte[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < bytes.Length; i++)
						bytes[i] = unchecked((byte)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => bytes[i] = unchecked((byte)(minimum + i)));
					Random.Shared.Shuffle(bytes);
				}

				return ValueTask.FromResult<object>(bytes);
			case TypeCode.SByte:
				var sbytes = new sbyte[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < sbytes.Length; i++)
						sbytes[i] = unchecked((sbyte)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => sbytes[i] = unchecked((sbyte)(minimum + i)));
					Random.Shared.Shuffle(sbytes);
				}

				return ValueTask.FromResult<object>(sbytes);
			case TypeCode.Int16:
				var resultInt16 = new short[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultInt16.Length; i++)
						resultInt16[i] = unchecked((short)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultInt16[i] = unchecked((short)(minimum + i)));
					Random.Shared.Shuffle(resultInt16);
				}

				return ValueTask.FromResult<object>(resultInt16);
			case TypeCode.Int32:
				var resultInt32 = new int[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultInt32.Length; i++)
						resultInt32[i] = minimum + i;
				}
				else
				{
					Parallel.For(0, count, i => resultInt32[i] = minimum + i);
					Random.Shared.Shuffle(resultInt32);
				}

				return ValueTask.FromResult<object>(resultInt32);
			case TypeCode.Int64:
				var resultInt64 = new long[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultInt64.Length; i++)
						resultInt64[i] = (long)minimum + i;
				}
				else
				{
					Parallel.For(0, count, i => resultInt64[i] = (long)minimum + i);
					Random.Shared.Shuffle(resultInt64);
				}

				return ValueTask.FromResult<object>(resultInt64);
			case TypeCode.UInt16:
				var resultUInt16 = new ushort[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultUInt16.Length; i++)
						resultUInt16[i] = unchecked((ushort)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultUInt16[i] = unchecked((ushort)(minimum + i)));
					Random.Shared.Shuffle(resultUInt16);
				}

				return ValueTask.FromResult<object>(resultUInt16);
			case TypeCode.UInt32:
				var resultUInt32 = new uint[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultUInt32.Length; i++)
						resultUInt32[i] = unchecked((uint)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultUInt32[i] = unchecked((uint)(minimum + i)));
					Random.Shared.Shuffle(resultUInt32);
				}

				return ValueTask.FromResult<object>(resultUInt32);
			case TypeCode.UInt64:
				var resultUInt64 = new ulong[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultUInt64.Length; i++)
						resultUInt64[i] = unchecked((ulong)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultUInt64[i] = unchecked((ulong)(minimum + i)));
					Random.Shared.Shuffle(resultUInt64);
				}

				return ValueTask.FromResult<object>(resultUInt64);
			case TypeCode.Single:
				var resultSingle = new float[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultSingle.Length; i++)
						resultSingle[i] = unchecked((float)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultSingle[i] = unchecked((float)(minimum + i)));
					Random.Shared.Shuffle(resultSingle);
				}

				return ValueTask.FromResult<object>(resultSingle);
			case TypeCode.Double:
				var resultDouble = new double[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultDouble.Length; i++)
						resultDouble[i] = unchecked((double)(minimum + i));
				}
				else
				{
					Parallel.For(0, count, i => resultDouble[i] = unchecked((double)(minimum + i)));
					Random.Shared.Shuffle(resultDouble);
				}

				return ValueTask.FromResult<object>(resultDouble);
			case TypeCode.Decimal:
				var resultDecimal = new decimal[count];

				if(context.Options.Switch(ORDERED_OPTION))
				{
					for(int i = 0; i < resultDecimal.Length; i++)
						resultDecimal[i] = decimal.Add(minimum, i);
				}
				else
				{
					Parallel.For(0, count, i => resultDecimal[i] = decimal.Add(minimum, i));
					Random.Shared.Shuffle(resultDecimal);
				}

				return ValueTask.FromResult<object>(resultDecimal);
			default:
				throw new CommandException($"The '{TYPE_OPTION}' option of this command does not support the specified '{type.Name}' value.");
		}
	}
	#endregion
}
