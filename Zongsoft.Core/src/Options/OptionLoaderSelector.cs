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
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Options
{
	public class OptionLoaderSelector : IOptionLoaderSelector
	{
		#region 成员字段
		private OptionNode _root;
		private readonly ConcurrentDictionary<Type, IOptionLoader> _loaders;
		#endregion

		#region 构造函数
		public OptionLoaderSelector(OptionNode root)
		{
			if(root == null)
				throw new ArgumentNullException("root");

			_root = root;
			_loaders = new ConcurrentDictionary<Type, IOptionLoader>();
		}
		#endregion

		#region 公共属性
		public OptionNode RootNode
		{
			get
			{
				return _root;
			}
		}
		#endregion

		#region 公共方法
		public void Register(Type providerType, IOptionLoader loader)
		{
			if(providerType == null)
				throw new ArgumentNullException("providerType");

			if(loader == null)
				throw new ArgumentNullException("loader");

			_loaders[providerType] = loader;
		}

		public void Register(Type providerType, Type loaderType)
		{
			if(providerType == null)
				throw new ArgumentNullException("providerType");

			if(loaderType == null)
				throw new ArgumentNullException("loaderType");

			var loader = this.CreateLoader(loaderType);

			if(loader == null)
				throw new InvalidOperationException("Can't create a instance with the loaderType.");

			_loaders[providerType] = loader;
		}

		public IOptionLoader GetLoader(IOptionProvider provider)
		{
			if(provider == null)
				return null;

			IOptionLoader loader;
			var providerType = provider.GetType();

			if(_loaders.TryGetValue(providerType, out loader) && loader != null)
				return loader;

			var attribute = (OptionLoaderAttribute)Attribute.GetCustomAttribute(providerType, typeof(OptionLoaderAttribute), true);

			if(attribute != null && attribute.LoaderType != null)
			{
				loader = this.CreateLoader(attribute.LoaderType);

				if(loader != null)
				{
					if(_loaders.TryAdd(providerType, loader))
						return loader;

					return _loaders[providerType];
				}
			}

			return null;
		}
		#endregion

		#region 虚拟方法
		protected virtual IOptionLoader CreateLoader(Type type)
		{
			if(type == null)
				return null;

			if(!typeof(IOptionLoader).IsAssignableFrom(type))
				throw new ArgumentException("The parameter is not a IOptionLoader type.");

			var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(OptionNode) }, null);

			try
			{
				if(constructor != null)
					return (IOptionLoader)constructor.Invoke(new object[] { _root });

				return (IOptionLoader)Activator.CreateInstance(type, true);
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format("Can create a option-loader instance from the '{0}' type.", type), ex);
			}
		}
		#endregion
	}
}
