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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration
{
	public class RedisConnectionSettingValuesMapper : ConnectionSettingValuesMapper
	{
		public RedisConnectionSettingValuesMapper() : base(RedisConnectionSetting.DRIVER, new[]
		{
			new KeyValuePair<string, string>(nameof(IConnectionSettingValues.Client), "name"),
			new KeyValuePair<string, string>(nameof(IConnectionSettingValues.UserName), "user"),
			new KeyValuePair<string, string>(nameof(IConnectionSettingValues.Timeout), "connectTimeout"),
			new KeyValuePair<string, string>(nameof(IConnectionSettingValues.Database), "defaultDatabase"),
			new KeyValuePair<string, string>(nameof(IConnectionSettingValues.Application), "serviceName"),
		}) { }
	}
}