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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Services;

namespace Zongsoft.Data.Templates
{
	public abstract class DataTemplateModelProviderBase : IDataTemplateModelProvider, IMatchable
	{
		#region 构造函数
		protected DataTemplateModelProviderBase(string name, IServiceProvider services)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Services = services ?? throw new ArgumentNullException(nameof(services));
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IServiceProvider Services { get; }
		#endregion

		#region 抽象方法
		public abstract IDataTemplateModel GetModel(IDataTemplate template, object argument);
		#endregion

		#region 服务匹配
		bool IMatchable.Match(object parameter) => this.OnMatch(parameter);
		protected virtual bool OnMatch(object parameter) => parameter switch
		{
			string name => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase),
			IDataTemplate template => string.Equals(template?.Name, this.Name, StringComparison.OrdinalIgnoreCase),
			_ => false,
		};
		#endregion
	}
}