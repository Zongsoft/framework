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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Metadata;

/// <summary>
/// 表示数据实体属性序号器的元数据类。
/// </summary>
internal class DataEntityPropertySequence : IDataEntityPropertySequence
{
	#region 静态字段
	private static readonly Regex _regex = new Regex(
		@"(?<name>(\#|\*|[\w].*))\s*(?<refs>\w+\s*(\,\s*\w+\s*)*)?\s*(\:\s*(?<seed>\d+))?\s*(/\s*(?<interval>\d+))?",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region 成员字段
	private IList<string> _referenceNames;
	private IDataEntitySimplexProperty[] _references;
	#endregion

	#region 构造函数
	public DataEntityPropertySequence(IDataEntitySimplexProperty property, string name, int seed, int interval = 1, IList<string> references = null)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Property = property ?? throw new ArgumentNullException(nameof(property));
		this.Name = name.Trim();
		this.Seed = seed;
		this.Interval = interval == 0 ? 1 : interval;

		if(this.Name == "#")
			this.Name = "#" + property.Entity.Name + ":" + property.Name;

		_referenceNames = references;
	}
	#endregion

	#region 公共属性
	/// <summary>获取序号所属的数据实体元素。</summary>
	public IDataEntity Entity => this.Property.Entity;

	/// <summary>获取序号所属的数据属性元素。</summary>
	public IDataEntitySimplexProperty Property { get; }

	/// <summary>获取序号器的名称。</summary>
	public string Name { get; }

	/// <summary>获取或设置序号器的种子数。</summary>
	public int Seed { get; set; }

	/// <summary>获取或设置序号器的递增量，默认为<c>1</c>。</summary>
	public int Interval { get; set; }

	/// <summary>获取一个值，指示是否采用数据库内置序号方案。</summary>
	public bool IsBuiltin => string.IsNullOrEmpty(this.Name) || this.Name[0] != '#';

	/// <summary>获取一个值，指示是否采用外置序号器方案。</summary>
	public bool IsExternal => this.Name != null && this.Name.Length > 0 && this.Name[0] == '#';

	/// <summary>获取序号的引用的属性数组。</summary>
	public IDataEntitySimplexProperty[] References
	{
		get
		{
			if(_references == null)
			{
				if(_referenceNames == null || _referenceNames.Count == 0)
					return null;

				var references = new IDataEntitySimplexProperty[_referenceNames.Count];

				for(int i = 0; i < references.Length; i++)
				{
					if(this.Entity.Properties.TryGetValue(_referenceNames[i], out var property) && property.IsSimplex)
						references[i] = (IDataEntitySimplexProperty)property;
					else
					{
						if(property == null)
							throw new DataException($"The specified '{_referenceNames[i]}' member of the '{this.Name}' sequence is a property that does not exist.");
						else
							throw new DataException($"The specified '{_referenceNames[i]}' member of the '{this.Name}' sequence must be a simplex property.");
					}
				}

				_references = references;
			}

			return _references;
		}
	}
	#endregion

	#region 静态方法
	public static IDataEntityPropertySequence Parse(IDataEntitySimplexProperty property, string text)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		if(string.IsNullOrEmpty(text))
			return null;

		var match = _regex.Match(text);

		if(!match.Success)
			return null;

		var name = match.Groups["name"].Value;

		//如果名字组是字母打头，则将其视为引用为代理序列。而代理序列不允许其他选项
		if(name != null && name.Length > 0 && char.IsLetter(name[0]))
		{
			var index = name.LastIndexOfAny(['.', ':']);

			if(index > 0 && index < name.Length - 1)
				return new Proxy(property, name[..index], name[(index + 1)..]);

			throw new DataException($"The specified '{name}' is an invalid sequence reference.");
		}

		int seed = 0, interval = 1;
		IList<string> references = null;

		if(match.Groups["seed"].Success)
			seed = int.Parse(match.Groups["seed"].Value);

		if(match.Groups["interval"].Success)
			interval = int.Parse(match.Groups["interval"].Value);

		if(match.Groups["refs"].Success)
			references = match.Groups["refs"].Value.Split(',');

		return new DataEntityPropertySequence(
					property,
					match.Groups["name"].Value,
					GetSeedDefaultValue(property.Type, seed),
					interval,
					references);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static int GetSeedDefaultValue(System.Data.DbType type, int seed)
	{
		if(seed != 0)
			return seed;

		switch(type)
		{
			case System.Data.DbType.Byte:
			case System.Data.DbType.SByte:
				return 10;
			case System.Data.DbType.Int16:
			case System.Data.DbType.UInt16:
				return 1000;
			case System.Data.DbType.Int32:
			case System.Data.DbType.UInt32:
			case System.Data.DbType.Single:
				return 100_000;
			case System.Data.DbType.Int64:
			case System.Data.DbType.UInt64:
			case System.Data.DbType.Double:
			case System.Data.DbType.Decimal:
			case System.Data.DbType.Currency:
				return 1000_0000;
			default:
				return 1;
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

		public int Seed
		{
			get => this.EnsureDestinationSequence().Seed;
			set => this.EnsureDestinationSequence().Seed = value;
		}

		public int Interval
		{
			get => this.EnsureDestinationSequence().Interval;
			set => this.EnsureDestinationSequence().Interval = value;
		}

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
