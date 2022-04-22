/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public class DataSourceSelector : IDataSourceSelector
	{
		#region 成员字段
		private readonly int _readerTotal;
		private readonly int _writerTotal;
		private readonly SourceToken[] _readers;
		private readonly SourceToken[] _writers;
		#endregion

		#region 私有构造
		public DataSourceSelector(IEnumerable<IDataSource> sources)
		{
			if(sources == null)
				throw new ArgumentNullException(nameof(sources));

			var readers = new List<SourceToken>();
			var writers = new List<SourceToken>();

			foreach(var source in sources)
			{
				var weight = GetWeight(source);

				if((source.Mode & DataAccessMode.ReadOnly) == DataAccessMode.ReadOnly)
				{
					_readerTotal += weight;
					readers.Add(new SourceToken(source, weight, _readerTotal));
				}

				if((source.Mode & DataAccessMode.WriteOnly) == DataAccessMode.WriteOnly)
				{
					_writerTotal += weight;
					writers.Add(new SourceToken(source, weight, _writerTotal));
				}
			}

			_readers = readers.ToArray();
			_writers = writers.ToArray();
		}
		#endregion

		#region 公共方法
		public IDataSource GetSource(IDataAccessContextBase context)
		{
			var sources = this.GetSources(context, out var total);

			if(sources.Length == 0)
				return null;
			if(sources.Length == 1)
				return sources[0].Source;

			var weight = Math.Abs(Zongsoft.Common.Randomizer.GenerateInt32()) % total;
			var position = Array.BinarySearch(sources, weight);

			return position >= 0 ?
				sources[position].Source :
				sources[(-position) - 1].Source;
		}
		#endregion

		#region 私有方法
		private SourceToken[] GetSources(IDataAccessContextBase context, out int total)
		{
			switch(context.Method)
			{
				case DataAccessMethod.Select:
				case DataAccessMethod.Exists:
				case DataAccessMethod.Aggregate:
					total = _readerTotal;
					return _readers;
				case DataAccessMethod.Execute:
					if(((DataExecuteContextBase)context).Command.Mutability == Metadata.CommandMutability.None)
					{
						total = _readerTotal;
						return _readers;
					}
					else
					{
						total = _writerTotal;
						return _writers;
					}
				default:
					total = _writerTotal;
					return _writers;
			}
		}

		private static int GetWeight(IDataSource source, int defaultValue = 100)
		{
			return source.Properties.TryGetValue("weight", out var value) && Zongsoft.Common.Convert.TryConvertValue<int>(value, out var weight) ? Math.Max(weight, 0) : defaultValue;
		}
		#endregion

		#region 嵌套结构
		private readonly struct SourceToken : IComparable<SourceToken>, IComparable<int>
		{
			public SourceToken(IDataSource source, int weight, int boundary)
			{
				this.Source = source;
				this.Weight = weight;
				this.Boundary = boundary;
			}

			public readonly IDataSource Source;
			public readonly int Weight;
			public readonly int Boundary;

			public int CompareTo(int other) => this.Boundary.CompareTo(other);
			public int CompareTo(SourceToken other) => this.Boundary.CompareTo(other.Boundary);
		}
		#endregion
	}
}
