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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security
{
	[Service(typeof(ICensorship))]
	public class Censorship : ICensorship
	{
		#region 常量定义
		private const string DATA_ENTITY_CENSORSHIP = "Security.Censorship";

		public const string KEY_NAMES = "Names";
		public const string KEY_SENSITIVES = "Sensitives";
		#endregion

		#region 成员字段
		private string[] _keys;
		private IDataAccess _dataAccess;
		private readonly IServiceProvider _services;
		#endregion

		#region 构造函数
		public Censorship(IServiceProvider services)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
		}

		public Censorship(IServiceProvider services, string[] keys)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_keys = keys;
		}

		public Censorship(IServiceProvider services, IEnumerable<string> keys)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));

			if(keys != null)
				_keys = keys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		}
		#endregion

		#region 公共属性
		public string[] Keys
		{
			get => _keys;
			set => _keys = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IDataAccess DataAccess
		{
			get => _dataAccess ??= _services.ResolveRequired<IDataAccessProvider>().GetAccessor(Modules.Security);
			set => _dataAccess = value ?? throw new ArgumentNullException(nameof(value));
		}
		#endregion

		#region 公共方法
		public bool IsBlocked(string word, params string[] keys)
		{
			if(string.IsNullOrWhiteSpace(word))
				return false;

			//处理空键参数
			if(keys == null || keys.Length < 1)
				keys = _keys;

			if(keys == null || keys.Length < 1)
				return this.DataAccess.Exists(DATA_ENTITY_CENSORSHIP, Condition.Equal("Word", word.Trim()));

			return this.DataAccess.Exists(DATA_ENTITY_CENSORSHIP, Condition.In("Name", keys) & Condition.Equal("Word", word.Trim()));
		}
		#endregion
	}
}
