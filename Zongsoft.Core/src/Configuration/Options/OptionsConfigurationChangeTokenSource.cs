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

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Options
{
	public class OptionsConfigurationChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
	{
		#region 成员字段
		private readonly IConfiguration _configuration;
		#endregion

		#region 构造函数
		public OptionsConfigurationChangeTokenSource(IConfiguration configuration) : this(string.Empty, configuration)
		{
		}

		public OptionsConfigurationChangeTokenSource(string name, IConfiguration configuration)
		{
			this.Name = name ?? string.Empty;
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}
		#endregion

		#region 公共属性
		public string Name { get; }

		public IChangeToken GetChangeToken()
		{
			return _configuration.GetReloadToken();
		}
		#endregion
	}
}
