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
		private readonly string _defaultDriver;
		private readonly Dictionary<string, DataSourceWeighter> _weighters;
		#endregion

		#region 私有构造
		public DataSourceSelector(IEnumerable<IDataSource> sources)
		{
			if(sources == null)
				throw new ArgumentNullException(nameof(sources));

			foreach(var source in sources)
			{
				if(string.IsNullOrEmpty(_defaultDriver))
					_defaultDriver = source.Driver.Name;

				if(!source.Name.Contains(DataSource.SEPARATOR))
				{
					_defaultDriver = source.Driver.Name;
					break;
				}
			}

			_weighters = new Dictionary<string, DataSourceWeighter>(StringComparer.OrdinalIgnoreCase);

			foreach(var group in sources.GroupBy(source => source.Driver.Name, StringComparer.OrdinalIgnoreCase))
				_weighters[group.Key] = new DataSourceWeighter(group);
		}
		#endregion

		#region 公共方法
		public IDataSource GetSource(IDataAccessContextBase context)
		{
			var driver = string.IsNullOrEmpty(context.Driver) ? _defaultDriver : context.Driver;

			if(driver != null && _weighters.TryGetValue(driver, out var weighter))
				return weighter.Get(context);

			return null;
		}
		#endregion

		#region 私有方法
		private static int GetWeight(IDataSource source, int defaultValue = 100)
		{
			return source.Properties.TryGetValue("weight", out var value) && Zongsoft.Common.Convert.TryConvertValue<int>(value, out var weight) ? Math.Max(weight, 0) : defaultValue;
		}
		#endregion

		#region 加权计重
		private class DataSourceWeighter
		{
			private readonly Components.Weighter<IDataSource> _readables;
			private readonly Components.Weighter<IDataSource> _writables;

			public DataSourceWeighter(IEnumerable<IDataSource> sources)
			{
				_readables = new Components.Weighter<IDataSource>(sources.Where(source => (source.Mode & DataAccessMode.ReadOnly) == DataAccessMode.ReadOnly), source => GetWeight(source));
				_writables = new Components.Weighter<IDataSource>(sources.Where(source => (source.Mode & DataAccessMode.WriteOnly) == DataAccessMode.WriteOnly), source => GetWeight(source));
			}

			public IDataSource Get(IDataAccessContextBase context)
			{
				return context.Method switch
				{
					DataAccessMethod.Select or DataAccessMethod.Exists or DataAccessMethod.Aggregate => _readables.Get(),
					DataAccessMethod.Execute => ((DataExecuteContextBase)context).Command.Mutability == Metadata.CommandMutability.None ? _readables.Get() : _writables.Get(),
					_ => _writables.Get(),
				};
			}
		}
		#endregion
	}
}
