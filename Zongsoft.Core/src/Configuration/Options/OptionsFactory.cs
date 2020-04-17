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
using System.Collections.Generic;

using Microsoft.Extensions.Options;

namespace Zongsoft.Configuration.Options
{
	public class OptionsFactory<TOptions> : IOptionsFactory<TOptions> where TOptions : class, new()
	{
		#region 私有变量
		private readonly IEnumerable<IConfigureOptions<TOptions>> _beforeConfigures;
		private readonly IEnumerable<IPostConfigureOptions<TOptions>> _afterConfigures;
		private readonly IEnumerable<IValidateOptions<TOptions>> _validations;
		#endregion

		#region 构造函数
		public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> configures, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures) : this(configures, postConfigures, validations: null)
		{
		}

		public OptionsFactory(IEnumerable<IConfigureOptions<TOptions>> configures, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, IEnumerable<IValidateOptions<TOptions>> validations)
		{
			_beforeConfigures = configures;
			_afterConfigures = postConfigures;
			_validations = validations;
		}
		#endregion

		#region 公共方法
		public TOptions Create(string name)
		{
			var options = this.OnCreate();

			foreach(var before in _beforeConfigures)
			{
				if(before is IConfigureNamedOptions<TOptions> namedConfigure)
					namedConfigure.Configure(name, options);
				else if(name == string.Empty)
					before.Configure(options);
			}

			foreach(var after in _afterConfigures)
			{
				after.PostConfigure(name, options);
			}

			if(_validations != null)
			{
				var failures = new List<string>();

				foreach(var validate in _validations)
				{
					var result = validate.Validate(name, options);

					if(result.Failed)
						failures.AddRange(result.Failures);
				}

				if(failures.Count > 0)
					throw new OptionsValidationException(name, typeof(TOptions), failures);
			}

			return options;
		}
		#endregion

		#region 虚拟方法
		protected virtual TOptions OnCreate()
		{
			var type = typeof(TOptions);

			if(type.IsInterface || type.IsAbstract)
				return Zongsoft.Data.Model.Build<TOptions>();

			return new TOptions();
		}
		#endregion
	}
}
