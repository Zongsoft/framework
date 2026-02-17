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
[CommandOption(SIZE_OPTION, 's', typeof(int), 10)]
[CommandOption(MINIMUM_OPTION, typeof(int))]
[CommandOption(MAXIMUM_OPTION, typeof(int))]
[CommandOption(POSITIVE_OPTION, 'p')]
[CommandOption(ENHANCED_OPTION, 'e')]
public class RandomCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string TYPE_OPTION = "type";
	private const string SIZE_OPTION = "size";
	private const string MINIMUM_OPTION = "min";
	private const string MAXIMUM_OPTION = "max";
	private const string POSITIVE_OPTION = "positive";
	private const string ENHANCED_OPTION = "enhanced";
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(!TypeAlias.TryParse(context.Options.GetValue<string>(TYPE_OPTION), out var type))
			throw new CommandOptionException(TYPE_OPTION);

		if(!context.Options.TryGetValue<int>(SIZE_OPTION, out var size) || size < 1)
			throw new CommandOptionException(SIZE_OPTION);

		var minimum = context.Options.Contains(MINIMUM_OPTION) ? context.Options.GetValue<int>(MINIMUM_OPTION) : default(int?);
		var maximum = context.Options.Contains(MAXIMUM_OPTION) ? context.Options.GetValue<int>(MAXIMUM_OPTION) : default(int?);
		var positive = context.Options.Switch(POSITIVE_OPTION);
		var randomizer = context.Options.Switch(ENHANCED_OPTION) ? Randomizer.Enhanced : Randomizer.Default;

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Char:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);

					var result = new char[size];
					for(int i = 0; i < bytes.Length; i++)
						result[i] = (char)Clamp(bytes[i], minimum, maximum);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>((char)Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, positive));
			case TypeCode.Byte:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);

					if(minimum.HasValue || maximum.HasValue)
					{
						for(int i=0; i< bytes.Length; i++)
							bytes[i] = Clamp(bytes[i], minimum, maximum);
					}

					return ValueTask.FromResult<object>(bytes);
				}

				return ValueTask.FromResult<object>(unchecked((byte)Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, true)));
			case TypeCode.SByte:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(size);
					var result = new sbyte[size];

					for(int i = 0; i < bytes.Length; i++)
						result[i] = Clamp((sbyte)bytes[i], minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((sbyte)Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, positive)));
			case TypeCode.Int16:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(short) * size);
					var result = new short[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToInt16(bytes, i * sizeof(short)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((short)Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, positive)));
			case TypeCode.Int32:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(int) * size);
					var result = new int[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToInt32(bytes, i * sizeof(int)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, positive));
			case TypeCode.Int64:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(long) * size);
					var result = new long[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToInt64(bytes, i * sizeof(long)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(BitConverter.ToInt64(randomizer.GetBytes(sizeof(long))), minimum, maximum, positive));
			case TypeCode.UInt16:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(ushort) * size);
					var result = new ushort[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToUInt16(bytes, i * sizeof(ushort)), minimum, maximum);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(unchecked((ushort)Clamp(randomizer.GetInt32(minimum, maximum), minimum, maximum, positive)));
			case TypeCode.UInt32:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(uint) * size);
					var result = new uint[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToUInt32(bytes, i * sizeof(uint)), minimum, maximum);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(BitConverter.ToUInt32(randomizer.GetBytes(sizeof(uint))), minimum, maximum));
			case TypeCode.UInt64:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(ulong) * size);
					var result = new ulong[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToUInt64(bytes, i * sizeof(ulong)), minimum, maximum);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(BitConverter.ToUInt64(randomizer.GetBytes(sizeof(ulong))), minimum, maximum));
			case TypeCode.Single:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(float) * size);
					var result = new float[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToSingle(bytes, i * sizeof(float)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(BitConverter.ToSingle(randomizer.GetBytes(sizeof(float))), minimum, maximum, positive));
			case TypeCode.Double:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(double) * size);
					var result = new double[size];

					for(int i = 0; i < size; i++)
						result[i] = Clamp(BitConverter.ToDouble(bytes, i * sizeof(double)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>(Clamp(BitConverter.ToDouble(randomizer.GetBytes(sizeof(double))), minimum, maximum, positive));
			case TypeCode.Decimal:
				if(size > 1)
				{
					var bytes = randomizer.GetBytes(sizeof(double) * size);
					var result = new decimal[size];

					for(int i = 0; i < size; i++)
						result[i] = (decimal)Clamp(BitConverter.ToDouble(bytes, i * sizeof(double)), minimum, maximum, positive);

					return ValueTask.FromResult<object>(result);
				}

				return ValueTask.FromResult<object>((decimal)Clamp(BitConverter.ToDouble(randomizer.GetBytes(sizeof(double))), minimum, maximum, positive));
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

	#region 私有方法
	private static byte Clamp(byte value, int? min, int? max)
	{
		if(min < 0)
			min = 0;
		if(max < 0)
			max = 0;

		if(min.HasValue)
			return max.HasValue ?
				(byte)Math.Clamp(value, min.Value, max.Value):
				(byte)Math.Max(value, min.Value);
		else
			return max.HasValue ? (byte)Math.Min(value, max.Value) : value;
	}

	private static sbyte Clamp(sbyte value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(sbyte)Math.Clamp(value, min.Value, max.Value) :
				(sbyte)Math.Max(value, min.Value);
		else
			return max.HasValue ? (sbyte)Math.Min(value, max.Value) : value;
	}

	private static short Clamp(short value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(short)Math.Clamp(value, min.Value, max.Value) :
				(short)Math.Max(value, min.Value);
		else
			return max.HasValue ? (short)Math.Min(value, max.Value) : value;
	}

	private static ushort Clamp(ushort value, int? min, int? max)
	{
		if(min < 0)
			min = 0;
		if(max < 0)
			max = 0;

		if(min.HasValue)
			return max.HasValue ?
				(ushort)Math.Clamp(value, min.Value, max.Value) :
				(ushort)Math.Max(value, min.Value);
		else
			return max.HasValue ? (ushort)Math.Min(value, max.Value) : value;
	}

	private static int Clamp(int value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(int)Math.Clamp(value, min.Value, max.Value) :
				(int)Math.Max(value, min.Value);
		else
			return max.HasValue ? (int)Math.Min(value, max.Value) : value;
	}

	private static uint Clamp(uint value, int? min, int? max)
	{
		if(min < 0)
			min = 0;
		if(max < 0)
			max = 0;

		if(min.HasValue)
			return max.HasValue ?
				(uint)Math.Clamp(value, min.Value, max.Value) :
				(uint)Math.Max(value, min.Value);
		else
			return max.HasValue ? (uint)Math.Min(value, max.Value) : value;
	}

	private static long Clamp(long value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(long)Math.Clamp(value, min.Value, max.Value) :
				(long)Math.Max(value, min.Value);
		else
			return max.HasValue ? (long)Math.Min(value, max.Value) : value;
	}

	private static ulong Clamp(ulong value, int? min, int? max)
	{
		if(min < 0)
			min = 0;
		if(max < 0)
			max = 0;

		if(min.HasValue)
			return max.HasValue ?
				(ulong)Math.Clamp(value, (ulong)min.Value, (ulong)max.Value) :
				(ulong)Math.Max(value, (ulong)min.Value);
		else
			return max.HasValue ? (ulong)Math.Min(value, (ulong)max.Value) : value;
	}

	private static float Clamp(float value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(float)Math.Clamp(value, min.Value, max.Value) :
				(float)Math.Max(value, min.Value);
		else
			return max.HasValue ? (float)Math.Min(value, max.Value) : value;
	}

	private static double Clamp(double value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(double)Math.Clamp(value, min.Value, max.Value) :
				(double)Math.Max(value, min.Value);
		else
			return max.HasValue ? (double)Math.Min(value, max.Value) : value;
	}

	private static decimal Clamp(decimal value, int? min, int? max, bool positive)
	{
		if(positive)
			value = Math.Abs(value);

		if(min.HasValue)
			return max.HasValue ?
				(decimal)Math.Clamp(value, min.Value, max.Value) :
				(decimal)Math.Max(value, min.Value);
		else
			return max.HasValue ? (decimal)Math.Min(value, max.Value) : value;
	}
	#endregion

	private abstract class Randomizer
	{
		public static readonly Randomizer Default = new DefaultRandomizer();
		public static readonly Randomizer Enhanced = new EnhancedRandomizer();

		public abstract int GetInt32();
		public abstract int GetInt32(int? minimum, int? maximum);
		public abstract byte[] GetBytes(int count);

		sealed class DefaultRandomizer : Randomizer
		{
			public override int GetInt32() => Random.Shared.Next();
			public override int GetInt32(int? minimum, int? maximum) => Random.Shared.Next(minimum ?? int.MinValue, maximum ?? int.MaxValue);
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
			public override int GetInt32(int? minimum, int? maximum) => System.Security.Cryptography.RandomNumberGenerator.GetInt32(minimum ?? int.MinValue, maximum ?? int.MaxValue);
			public override byte[] GetBytes(int count) => System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);
		}
	}
}
