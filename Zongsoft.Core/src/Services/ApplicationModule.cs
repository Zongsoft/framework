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

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Collections;
using Zongsoft.ComponentModel;

namespace Zongsoft.Services
{
	[System.Reflection.DefaultMember(nameof(Schemas))]
	public class ApplicationModule : IApplicationModule, IMatchable<string>, IDisposable
	{
		#region 成员字段
		private readonly object _syncRoot = new object();
		private ServiceProvider _services;
		#endregion

		#region 构造函数
		public ApplicationModule(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = this.Title = name.Trim();
			this.Schemas = new SchemaCollection();
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		public ApplicationModule(string name, string title, string description = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.Title = title ?? this.Name;
			this.Description = description;
			this.Schemas = new SchemaCollection();
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get; protected set;
		}

		public string Title
		{
			get; set;
		}

		public string Description
		{
			get; set;
		}

		public virtual IServiceProvider Services
		{
			get
			{
				if(_services == null)
				{
					lock(_syncRoot)
					{
						if(_services == null)
							_services = new ServiceProvider(this.Name, ApplicationContext.Current.Services.CreateScope().ServiceProvider);
					}
				}

				return _services;
			}
		}

		public INamedCollection<Schema> Schemas
		{
			get;
		}

		public IDictionary<string, object> Properties
		{
			get;
		}
		#endregion

		#region 匹配方法
		bool IMatchable.Match(object parameter)
		{
			return parameter switch
			{
				string text => this.Name.Equals(text, StringComparison.OrdinalIgnoreCase),
				_ => false,
			};
		}

		bool IMatchable<string>.Match(string parameter)
		{
			return this.Name.Equals(parameter, StringComparison.OrdinalIgnoreCase);
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			_services?.Dispose();
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Title) || string.Equals(this.Name, this.Title))
				return this.Name;
			else
				return string.Format("[{0}] {1}", this.Name, this.Title);
		}
		#endregion
	}
}
