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

[CommandOption(TYPE_OPTION, 't', typeof(string), "byte")]
[CommandOption(SIZE_OPTION, 's', typeof(int), 1)]
[CommandOption(ENHANCED_OPTION, 'e')]
public class RandomCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string TYPE_OPTION = "type";
	private const string SIZE_OPTION = "size";
	private const string ENHANCED_OPTION = "enhanced";
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(!TypeAlias.TryParse(context.Options.GetValue<string>(TYPE_OPTION), out var type))
			throw new CommandOptionException(TYPE_OPTION);

		if(!context.Options.TryGetValue<int>(SIZE_OPTION, out var size) || size < 1)
			throw new CommandOptionException(SIZE_OPTION);

		var randomizer = context.Options.Switch(ENHANCED_OPTION) ? Randomizer.Enhanced : Randomizer.Default;

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Char:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);

					var result = new char[size];
					for(int i = 0; i < bytes.Length; i++)
						result[i] = (char)bytes[i];

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>((char)Random.Shared.Next());
			case TypeCode.Byte:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);
					return ValueTask.FromResult<object>(bytes);
				}

				return ValueTask.FromResult<object>(unchecked((byte)randomizer.GetInt32()));
			case TypeCode.SByte:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);
					var result = new sbyte[size];

					for(int i = 0; i < bytes.Length; i++)
						result[i] = (sbyte)bytes[i];

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((sbyte)randomizer.GetInt32()));
			case TypeCode.Int16:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(short) * size);
					var result = new short[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToInt16(bytes, i * sizeof(short));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((short)randomizer.GetInt32()));
			case TypeCode.Int32:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(int) * size);
					var result = new int[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToInt32(bytes, i * sizeof(int));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(randomizer.GetInt32());
			case TypeCode.Int64:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(long) * size);
					var result = new long[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToInt64(bytes, i * sizeof(long));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(BitConverter.ToInt64(randomizer.GetBytes(sizeof(long))));
			case TypeCode.UInt16:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(ushort) * size);
					var result = new ushort[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToUInt16(bytes, i * sizeof(ushort));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((ushort)randomizer.GetInt32()));
			case TypeCode.UInt32:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(uint) * size);
					var result = new uint[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToUInt32(bytes, i * sizeof(uint));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(BitConverter.ToUInt32(randomizer.GetBytes(sizeof(uint))));
			case TypeCode.UInt64:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(ulong) * size);
					var result = new ulong[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToUInt64(bytes, i * sizeof(ulong));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(BitConverter.ToUInt64(randomizer.GetBytes(sizeof(ulong))));
			case TypeCode.Single:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(float) * size);
					var result = new float[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToSingle(bytes, i * sizeof(float));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(BitConverter.ToSingle(randomizer.GetBytes(sizeof(float))));
			case TypeCode.Double:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(double) * size);
					var result = new double[size];

					for(int i = 0; i < size; i++)
						result[i] = BitConverter.ToDouble(bytes, i * sizeof(double));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(BitConverter.ToDouble(randomizer.GetBytes(sizeof(double))));
			case TypeCode.Decimal:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(double) * size);
					var result = new decimal[size];

					for(int i = 0; i < size; i++)
						result[i] = (decimal)BitConverter.ToDouble(bytes, i * sizeof(double));

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>((decimal)BitConverter.ToDouble(randomizer.GetBytes(sizeof(double))));
			case TypeCode.Boolean:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);
					var result = new bool[size];

					for(int i = 0; i < bytes.Length; i++)
						result[i] = (bytes[i] & 1) == 1;

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>((Random.Shared.Next() & 1) == 1);
			case TypeCode.String:
				return ValueTask.FromResult<object>(
					context.Options.Switch(ENHANCED_OPTION) ?
						Common.Randomizer.GenerateSecret(size):
						Common.Randomizer.GenerateString(size));
			default:
				throw new CommandException($"The '{TYPE_OPTION}' option of this command does not support the specified '{type.Name}' value.");
		}
	}
	#endregion

	abstract class Randomizer
	{
		public static readonly Randomizer Default = new DefaultRandomizer();
		public static readonly Randomizer Enhanced = new EnhancedRandomizer();

		public abstract int GetInt32();
		public abstract byte[] GetBytes(int count);

		sealed class DefaultRandomizer : Randomizer
		{
			public override int GetInt32() => Random.Shared.Next();
			public override byte[] GetBytes(int count)
			{
				var bytes = new byte[count];
				Random.Shared.NextBytes(bytes);
				return bytes;
			}
		}

		sealed class EnhancedRandomizer : Randomizer
		{
			public override int GetInt32() => System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
			public override byte[] GetBytes(int count) => System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);
		}
	}
}
