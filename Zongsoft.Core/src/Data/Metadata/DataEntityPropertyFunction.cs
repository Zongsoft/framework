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
		this.Name = name;
		this.Arguments = arguments;
	}
	#endregion

	#region 实例属性
	public string Name { get; }
	public string[] Arguments { get; }
	public bool HasArguments => this.Arguments != null && this.Arguments.Length > 0;
	#endregion

	#region 抽象方法
	public abstract object Execute(IDataEntitySimplexProperty property);
	#endregion

	#region 静态方法
	public static DataEntityPropertyFunction Get(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty)
			return null;

		text = text.Trim();
		var index = text.IndexOf('(');

		if(index < 0)
		{
			if(Builders.TryGetValue(text.ToString(), out var builder))
				return builder.Build();
		}
		else
		{
			if(text[^1] != ')')
				return null;

			var name = text[..index].Trim();
			var arguments = text.Slice(index + 1, text.Length - index - 2).ToString()
				.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			if(Builders.TryGetValue(name.ToString(), out var builder))
				return builder.Build(arguments);
		}

		throw new DataException($"Unrecognized {text} function.");
	}
	#endregion

	#region 嵌套子类
	private sealed class Now(params string[] arguments) : DataEntityPropertyFunction(nameof(Now), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Now Instance = new();

		private static readonly Now _now_ = new();
		private static readonly Now _utc_ = new("utc");

		public DataEntityPropertyFunction Build(params string[] arguments)
		{
			if(arguments == null || arguments.Length == 0)
				return _now_;
			if(string.Equals(arguments[0], "utc", StringComparison.OrdinalIgnoreCase))
				return _utc_;

			return new Now(arguments);
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

		public DataEntityPropertyFunction Build(params string[] arguments) => Instance;
		public override object Execute(IDataEntitySimplexProperty property) => DateTime.Today;
	}

	private sealed class Guid(params string[] arguments) : DataEntityPropertyFunction(nameof(Guid), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Guid Instance = new();

		public DataEntityPropertyFunction Build(params string[] arguments) => Instance;
		public override object Execute(IDataEntitySimplexProperty property) => System.Guid.NewGuid();
	}

	private sealed class Random(params string[] arguments) : DataEntityPropertyFunction(nameof(Random), arguments), IDataEntityPropertyFunctionBuilder
	{
		public static readonly Random Instance = new();

		public DataEntityPropertyFunction Build(params string[] arguments) => arguments == null || arguments.Length == 0 ? Instance : new Random(arguments);
		public override object Execute(IDataEntitySimplexProperty property) => property.Type.DbType switch
		{
			DbType.Byte => Zongsoft.Common.Randomizer.Generate(1)[0],
			DbType.SByte => (sbyte)Zongsoft.Common.Randomizer.Generate(1)[0],
			DbType.Int16 => BitConverter.ToInt16(Zongsoft.Common.Randomizer.Generate(2), 0),
			DbType.UInt16 => BitConverter.ToUInt16(Zongsoft.Common.Randomizer.Generate(2), 0),
			DbType.Int32 => Zongsoft.Common.Randomizer.GenerateInt32(),
			DbType.UInt32 => (uint)Zongsoft.Common.Randomizer.GenerateInt32(),
			DbType.Int64 => Zongsoft.Common.Randomizer.GenerateInt64(),
			DbType.UInt64 => (ulong)Zongsoft.Common.Randomizer.GenerateInt64(),
			DbType.Single => BitConverter.ToSingle(Zongsoft.Common.Randomizer.Generate(4), 0),
			DbType.Double => BitConverter.ToDouble(Zongsoft.Common.Randomizer.Generate(8), 0),
			DbType.Decimal or
			DbType.Currency => new Decimal(Zongsoft.Common.Randomizer.GenerateInt64()),
			DbType.Date or
			DbType.Time or
			DbType.DateTime or
			DbType.DateTime2 => new DateTime(Zongsoft.Common.Randomizer.GenerateInt64()),
			DbType.DateTimeOffset => new DateTimeOffset(Zongsoft.Common.Randomizer.GenerateInt64(), TimeSpan.Zero),
			DbType.Binary => Zongsoft.Common.Randomizer.Generate(property.Length > 0 ? property.Length : 8),
			DbType.Guid => System.Guid.NewGuid(),
			_ => Zongsoft.Common.Randomizer.GenerateString(),
		};
	}

	public sealed class BuilderCollection() : KeyedCollection<string, IDataEntityPropertyFunctionBuilder>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(IDataEntityPropertyFunctionBuilder builder) => builder.Name;
	}
	#endregion
}
