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

namespace Zongsoft.Data.Metadata;

public class DataEntityPropertySequence : IDataEntityPropertySequence
{
	#region 成员字段
	private readonly DataPropertySequence _sequence;
	private readonly Lazy<IDataEntitySimplexProperty[]> _references;
	#endregion

	#region 私有构造
	private DataEntityPropertySequence(IDataEntitySimplexProperty property, string name, int seed, int interval, params string[] references)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		this.Property = property ?? throw new ArgumentNullException(nameof(property));
		name = name == "#" ? $"#{property.Entity.Name}:{property.Name}" : name;
		_sequence = new(name, seed, interval, references);

		_references = new(() =>
		{
			if(!_sequence.HasReferences)
				return null;

			var references = new IDataEntitySimplexProperty[_sequence.References.Length];

			for(int i = 0; i < references.Length; i++)
			{
				if(this.Entity.Properties.TryGetValue(_sequence.References[i], out var property) && property.IsSimplex)
					references[i] = (IDataEntitySimplexProperty)property;
				else
				{
					if(property == null)
						throw new DataException($"The specified '{_sequence.References[i]}' member of the '{this.Name}' sequence is a property that does not exist.");
					else
						throw new DataException($"The specified '{_sequence.References[i]}' member of the '{this.Name}' sequence must be a simplex property.");
				}
			}

			return references;
		}, true);
	}
	#endregion

	#region 公共属性
	/// <summary>获取序号所属的数据实体元素。</summary>
	public IDataEntity Entity => this.Property.Entity;
	/// <summary>获取序号所属的数据属性元素。</summary>
	public IDataEntitySimplexProperty Property { get; }
	/// <summary>获取序号器的名称。</summary>
	public string Name => _sequence.Name;
	/// <summary>获取序号器的种子数。</summary>
	public int Seed => _sequence.Seed;
	/// <summary>获取序号器的递增量，默认为<c>1</c>。</summary>
	public int Interval => _sequence.Interval;
	/// <summary>获取一个值，指示是否采用数据库内置序号方案。</summary>
	public bool IsBuiltin => _sequence.IsBuiltin;
	/// <summary>获取一个值，指示是否采用外置序号器方案。</summary>
	public bool IsExternal => _sequence.IsExternal;
	/// <summary>获取序号的引用的属性数组。</summary>
	public IDataEntitySimplexProperty[] References => _references.Value;
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Property}]{_sequence}";
	#endregion

	#region 静态方法
	public static IDataEntityPropertySequence Create(IDataEntitySimplexProperty property, string text)
	{
		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(text);

		if(DataPropertySequence.TryParse(text, out var sequence))
			return Create(property, sequence);

		throw new DataException($"The specified '{text}' is an invalid sequence within the '{property.Entity?.QualifiedName}.{property.Name}' entity property.");
	}

	public static IDataEntityPropertySequence Create(IDataEntitySimplexProperty property, DataPropertySequence sequence)
	{
		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNull(sequence.Name, nameof(sequence));

		if(IsProxy(sequence))
		{
			var index = sequence.Name.LastIndexOfAny(['.', ':']);

			if(index > 0 && index < sequence.Name.Length - 1)
				return new Proxy(property, sequence.Name[..index].TrimStart('#'), sequence.Name[(index + 1)..]);

			throw new DataException($"The specified '{sequence.Name}' is an invalid sequence reference.");
		}

		return new DataEntityPropertySequence(
					property,
					sequence.Name,
					GetSeed(property.Type, sequence.Seed),
					sequence.Interval,
					sequence.References);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static bool IsProxy(DataPropertySequence sequence) =>
			(sequence.Name.Length > 0 && char.IsLetter(sequence.Name[0])) ||
			(sequence.Name.Length > 1 && sequence.Name[0] == '#');

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static int GetSeed(System.Data.DbType type, int seed)
		{
			if(seed != 0)
				return seed;

			return type switch
			{
				System.Data.DbType.Byte or System.Data.DbType.SByte => 10,
				System.Data.DbType.Int16 or System.Data.DbType.UInt16 => 1000,
				System.Data.DbType.Int32 or System.Data.DbType.UInt32 or System.Data.DbType.Single => 100_000,
				System.Data.DbType.Int64 or System.Data.DbType.UInt64 or System.Data.DbType.Double or System.Data.DbType.Decimal or System.Data.DbType.Currency => 1000_0000,
				_ => 1,
			};
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class Proxy : IDataEntityPropertySequence
	{
		#region 成员字段
		private readonly IDataEntitySimplexProperty _host;
		private IDataEntityPropertySequence _destination;

		private readonly string _destinationEntity;
		private readonly string _destinationProperty;
		#endregion

		#region 构造函数
		public Proxy(IDataEntitySimplexProperty host, string destinationEntity, string destinationProperty)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			_destinationEntity = destinationEntity;
			_destinationProperty = destinationProperty;
		}
		#endregion

		#region 公共属性
		public string Name => $"{_host.Entity.Name}:{_host.Name}";
		public int Seed => this.EnsureDestinationSequence().Seed;
		public int Interval => this.EnsureDestinationSequence().Interval;
		public bool IsBuiltin => this.EnsureDestinationSequence().IsBuiltin;
		public bool IsExternal => this.EnsureDestinationSequence().IsExternal;
		public IDataEntitySimplexProperty Property => this.EnsureDestinationSequence().Property;
		public IDataEntitySimplexProperty[] References => this.EnsureDestinationSequence().References;
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private IDataEntityPropertySequence EnsureDestinationSequence()
		{
			if(_destination == null)
			{
				lock(this)
				{
					if(_destination == null)
					{
						var entity = _host.Entity.GetEntity(_destinationEntity);

						if(entity != null && entity.Properties.TryGetValue(_destinationProperty, out var property) && property.IsSimplex)
						{
							_destination = ((IDataEntitySimplexProperty)property).Sequence ??
								throw new DataException($"The '{_destinationEntity}:{_destinationProperty}' sequence referenced by the '{_host.Entity.Name}:{_host.Name}' property does not exist.");

							if(_destination.References != null && _destination.References.Length > 0)
								throw new DataException($"The sequence referenced by the '{_host.Entity.Name}:{_host.Name}' property cannot contain dependencies.");
						}
						else
							throw new DataException($"The '{_destinationEntity}:{_destinationProperty}' sequence reference specified by the '{_host.Entity.Name}:{_host.Name}' property does not exist.");
					}
				}
			}

			return _destination;
		}
		#endregion
	}
	#endregion
}
