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

namespace Zongsoft.Services
{
	public interface IServiceProvider : System.IServiceProvider
	{
		#region 事件定义
		event EventHandler<ServiceRegisteredEventArgs> Registered;
		event EventHandler<ServiceUnregisteredEventArgs> Unregistered;
		#endregion

		#region 属性定义
		/// <summary>
		/// 获取服务提供程序的名字。
		/// </summary>
		string Name
		{
			get;
		}

		/// <summary>
		/// 获取服务提供程序的存储器。
		/// </summary>
		IServiceStorage Storage
		{
			get;
		}
		#endregion

		#region 注册方法
		void Register(string name, Type serviceType);
		void Register(string name, Type serviceType, params Type[] contractTypes);

		void Register(string name, object service);
		void Register(string name, object service, params Type[] contractTypes);

		void Register(Type serviceType, params Type[] contractTypes);
		void Register(object service, params Type[] contractTypes);

		void Unregister(string name);
		#endregion

		#region 解析方法
		/// <summary>
		/// 获取指定名称的服务类型。
		/// </summary>
		/// <param name="name">指定的服务名称。</param>
		/// <returns>返回指定名称的服务的类型，如果返回空(null)则表示没有找到指定名称的服务。</returns>
		Type GetServiceType(string name);

		object Resolve(string name);
		object Resolve(Type type);
		object Resolve(Type type, object parameter);

		object ResolveRequired(string name);
		object ResolveRequired(Type type);
		object ResolveRequired(Type type, object parameter);

		T Resolve<T>();
		T Resolve<T>(object parameter);

		T ResolveRequired<T>();
		T ResolveRequired<T>(object parameter);

		IEnumerable<object> ResolveAll(Type type);
		IEnumerable<object> ResolveAll(Type type, object parameter);

		IEnumerable<T> ResolveAll<T>();
		IEnumerable<T> ResolveAll<T>(object parameter);
		#endregion
	}
}
