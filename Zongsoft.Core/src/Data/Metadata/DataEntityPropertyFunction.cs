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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public interface IDataEntityPropertyFunctionBuilder
{
	string Name { get; }
	DataEntityPropertyFunction Build(params string[] arguments);
}

public abstract class DataEntityPropertyFunction
{
	#region 静态字段
	public static readonly BuilderCollection Builders =
	[
		Now.Instance,
		Guid.Instance,
		Today.Instance,
		Random.Instance,
	];
	#endregion

	#region 构造函数
	protected DataEntityPropertyFunction(string name, params string[] arguments)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.Name = name;
		this.Arguments = arguments;
	}
	#endregion

	#region 实例属性
	/// <summary>获取函数的名称。</summary>
	public string Name { get; }
	/// <summary>获取函数的参数集。</summary>
	public string[] Arguments { get; }
	/// <summary>获取一个值，指示是否有函数参数。</summary>
	public bool HasArguments => this.Arguments != null && this.Arguments.Length > 0;
	#endregion

	#region 抽象方法
	public abstract object Execute(IDataEntitySimplexProperty property);
	#endregion

	#region 重写方法
	public override string ToString() => this.HasArguments ? $"{this.Name}({string.Join(',', this.Arguments)})" : $"{this.Name}()";
	#endregion

	#region 静态方法
	public static DataEntityPropertyFunction Get(ReadOnlySpan<char> text) => DataPropertyFunction.TryParse(text, out var function) ? Get(function) : null;
	public static DataEntityPropertyFunction Get(DataPropertyFunction function)
	{
		if(string.IsNullOrEmpty(function.Name))
			return null;

		if(Builders.TryGetValue(function.Name, out var builder))
			return builder.Build(function.Arguments);

		throw new DataException($"Unrecognized {function.Name} function.");
	}

	public static bool TryGet(ReadOnlySpan<char> text, out DataEntityPropertyFunction result)
	{
		result = null;
		return DataPropertyFunction.TryParse(text, out var function) && TryGet(function, out result);
	}

	public static bool TryGet(DataPropertyFunction function, out DataEntityPropertyFunction result)
	{
		if(function.Name != null && Builders.TryGetValue(function.Name, out var builder))
		{
			result = builder.Build(function.Arguments);
			return true;
		}

		result = null;
		return false;
	}
	#endregion

	#region 符号重写
	public static explicit operator DataEntityPropertyFunction(DataPropertyFunction function) => Get(function);
	public static implicit operator DataPropertyFunction(DataEntityPropertyFunction function) => new(function.Name, function.Arguments);
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static DataException GetNotSupportedArgumentsEexception(string name, string[] arguments) => new($"The '{name}' function does not support the specified '({string.Join(',', arguments)})' argument(s).");
	#endregion

	#region 嵌套子类
	private sealed class Now(params string[] arguments) : DataEntityPropertyFunction(nameof(Now), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Now Instance = new();

		private static readonly Now _local_ = new();
		private static readonly Now _utc_ = new("utc");

		DataEntityPropertyFunction IDataEntityPropertyFunctionBuilder.Build(params string[] arguments)
		{
			if(arguments == null || arguments.Length == 0)
				return _local_;
			if(string.Equals(arguments[0], "utc", StringComparison.OrdinalIgnoreCase))
				return _utc_;

			throw GetNotSupportedArgumentsEexception(this.Name, arguments);
		}

		public override object Execute(IDataEntitySimplexProperty property)
		{
			if(this.HasArguments && string.Equals(this.Arguments[0], "utc", StringComparison.OrdinalIgnoreCase))
				return DateTime.UtcNow;
			else
				return DateTime.Now;
		}
	}

	private sealed class Today(params string[] arguments) : DataEntityPropertyFunction(nameof(Today), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Today Instance = new();

		private static readonly Today _local_ = new();
		private static readonly Today _utc_ = new("utc");

		DataEntityPropertyFunction IDataEntityPropertyFunctionBuilder.Build(params string[] arguments)
		{
			if(arguments == null || arguments.Length == 0)
				return _local_;
			if(string.Equals(arguments[0], "utc", StringComparison.OrdinalIgnoreCase))
				return _utc_;

			throw GetNotSupportedArgumentsEexception(this.Name, arguments);
		}

		public override object Execute(IDataEntitySimplexProperty property)
		{
			if(this.HasArguments && string.Equals(this.Arguments[0], "utc", StringComparison.OrdinalIgnoreCase))
				return DateTime.UtcNow.Date;
			else
				return DateTime.Today;
		}
	}

	private sealed class Guid(params string[] arguments) : DataEntityPropertyFunction(nameof(Guid), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Guid Instance = new();

		#if NET9_0_OR_GREATER
		private static readonly Guid _sequential_ = new("sequential");
		#endif

		DataEntityPropertyFunction IDataEntityPropertyFunctionBuilder.Build(params string[] arguments)
		{
			if(Common.ArrayExtension.IsEmpty(arguments))
				return Instance;

			#if NET9_0_OR_GREATER
			if(string.Equals(arguments[0], "sequential", StringComparison.OrdinalIgnoreCase))
				return _sequential_;
			#endif

			throw GetNotSupportedArgumentsEexception(this.Name, arguments);
		}

		public override object Execute(IDataEntitySimplexProperty property)
		{
			#if NET9_0_OR_GREATER
			if(this.HasArguments && string.Equals(this.Arguments[0], "sequential", StringComparison.OrdinalIgnoreCase))
				return System.Guid.CreateVersion7();
			#endif

			return System.Guid.NewGuid();
		}
	}

	private sealed class Random(params string[] arguments) : DataEntityPropertyFunction(nameof(Random), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Random Instance = new();

		DataEntityPropertyFunction IDataEntityPropertyFunctionBuilder.Build(params string[] arguments) =>
			Common.ArrayExtension.IsEmpty(arguments) ? Instance : new Random(arguments);

		public override object Execute(IDataEntitySimplexProperty property)
		{
			if(property == null || property.Type == null)
				return null;

			return property.Type.DbType switch
			{
				DbType.Guid => System.Guid.NewGuid(),
				DbType.Byte => Common.Randomizer.Generate(1)[0],
				DbType.SByte => (sbyte)Common.Randomizer.Generate(1)[0],
				DbType.Int16 => Common.Randomizer.GenerateInt16(),
				DbType.UInt16 => Common.Randomizer.GenerateUInt16(),
				DbType.Int32 => Common.Randomizer.GenerateInt32(),
				DbType.UInt32 => Common.Randomizer.GenerateUInt32(),
				DbType.Int64 => Common.Randomizer.GenerateInt64(),
				DbType.UInt64 => Common.Randomizer.GenerateUInt64(),
				DbType.Single => BitConverter.Int32BitsToSingle(Common.Randomizer.GenerateInt32()),
				DbType.Double => BitConverter.Int64BitsToDouble(Common.Randomizer.GenerateInt64()),
				DbType.Decimal or
				DbType.Currency => new Decimal(Common.Randomizer.GenerateInt64()),
				DbType.Date or
				DbType.Time or
				DbType.DateTime or
				DbType.DateTime2 => new DateTime(Common.Randomizer.GenerateInt64()),
				DbType.DateTimeOffset => new DateTimeOffset(Common.Randomizer.GenerateInt64(), TimeSpan.Zero),
				DbType.Binary => this.HasArguments ? Common.Randomizer.Generate(int.Parse(this.Arguments[0])) : Common.Randomizer.Generate(property.Length > 0 ? property.Length : 8),
				DbType.AnsiString or
				DbType.AnsiStringFixedLength or
				DbType.String or
				DbType.StringFixedLength => this.HasArguments ? Common.Randomizer.GenerateString(int.Parse(this.Arguments[0])) : Common.Randomizer.GenerateString(),
				_ => null,
			};
		}
	}

	public sealed class BuilderCollection() : KeyedCollection<string, IDataEntityPropertyFunctionBuilder>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(IDataEntityPropertyFunctionBuilder builder) => builder.Name;
	}
	#endregion
}
