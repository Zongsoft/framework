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
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Services;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Metadata.Profiles;

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供数据访问操作的环境信息。
	/// </summary>
	[DefaultMember(nameof(Accessors))]
	[Service(Members = $"{nameof(Accessors)},{nameof(Filters)}")]
	public static class DataEnvironment
	{
		#region 成员字段
		private static IDataAccessProvider _accessors;
		private static IDataProviderFactory _providers;
		private static IDataValidatorProvider _validators;
		private static IDataPopulatorProviderFactory _populators;
		private static readonly ICollection<IDataMetadataLoader> _loaders;
		private static readonly KeyedCollection<string, IDataDriver> _drivers;
		private static readonly DataAccessFilterCollection _filters;
		#endregion

		#region 静态构造
		static DataEnvironment()
		{
			_accessors = DataAccessProvider.Instance;
			_providers = DataProviderFactory.Instance;
			_validators = DataValidatorProvider.Instance;
			_populators = DataPopulatorProviderFactory.Instance;
			_drivers = new DataDriverCollection();
			_filters = new DataAccessFilterCollection();
			_loaders = new List<IDataMetadataLoader>() { MetadataFileLoader.Default };
		}
		#endregion

		#region 公共属性
		public static IDataAccessProvider Accessors
		{
			get => _accessors;
			set => _accessors = value ?? throw new ArgumentNullException();
		}

		public static IDataProviderFactory Providers
		{
			get => _providers;
			set => _providers = value ?? throw new ArgumentNullException();
		}

		public static IDataValidatorProvider Validators
		{
			get => _validators;
			set => _validators = value ?? throw new ArgumentNullException();
		}

		public static IDataPopulatorProviderFactory Populators
		{
			get => _populators;
			set => _populators = value ?? throw new ArgumentNullException();
		}

		public static ICollection<IDataMetadataLoader> Loaders => _loaders;
		public static KeyedCollection<string, IDataDriver> Drivers => _drivers;
		public static DataAccessFilterCollection Filters => _filters;
		#endregion
	}
}
