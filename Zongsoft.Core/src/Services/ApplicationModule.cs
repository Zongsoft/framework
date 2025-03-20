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
using System.Reflection;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.ComponentModel;

namespace Zongsoft.Services
{
	[DefaultMember(nameof(Schemas))]
	public class ApplicationModule : IApplicationModule, IMatchable, IDisposable
	{
		#region 成员字段
		private string _title;
		private string _description;
		private ServiceProvider _services;
		private readonly object _syncRoot = new object();
		#endregion

		#region 构造函数
		public ApplicationModule(string name, string title = null, string description = null)
		{
			this.Name = name == null ? string.Empty : name.Trim();
			this.Title = title;
			this.Description = description;
			this.Schemas = new SchemaCollection();
			this.Properties = new Parameters();
		}
		#endregion

		#region 公共属性
		public string Name { get; protected set; }
		public SchemaCollection Schemas { get; }
		public Parameters Properties { get; }

		public string Title
		{
			get => string.IsNullOrEmpty(_title) ? Resources.ResourceUtility.GetResourceString(this.GetType(), [$"{this.Name}.{nameof(this.Title)}", this.Name]) : _title;
			set => _title = value;
		}

		public string Description
		{
			get => string.IsNullOrEmpty(_description) ? Resources.ResourceUtility.GetResourceString(this.GetType(), $"{this.Name}.{nameof(this.Description)}") : _description;
			set => _description = value;
		}

		public virtual IServiceProvider Services
		{
			get
			{
				if(_services == null)
				{
					lock(_syncRoot)
					{
						_services ??= new ServiceProvider(this.Name, ApplicationContext.Current.Services.CreateScope().ServiceProvider);
					}
				}

				return _services;
			}
		}
		#endregion

		#region 匹配方法
		bool IMatchable.Match(object parameter) => parameter != null && string.Equals(this.Name, parameter.ToString(), StringComparison.OrdinalIgnoreCase);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) => _services?.Dispose();
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Title) || string.Equals(this.Name, this.Title))
				return this.Name;
			else
				return $"[{this.Name}]{this.Title}";
		}
		#endregion
	}

	public class ApplicationModule<TEvents> : ApplicationModule where TEvents : EventRegistryBase, new()
	{
		#region 构造函数
		public ApplicationModule(string name, string title = null, string description = null) : base(name, title, description)
        {
			this.Events = new TEvents();
        }
		#endregion

		#region 公共属性
		/// <summary>获取本模块的事件注册表。</summary>
		public TEvents Events { get; }
		#endregion
	}
}
