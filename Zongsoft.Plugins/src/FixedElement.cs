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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Plugins
{
	public abstract class FixedElement : PluginElement
	{
		#region 私有变量
		private readonly object _syncRoot;
		#endregion

		#region 成员变量
		private Type _type;
		private string _typeName;
		private FixedElementType _fixedElementType;
		private object _value;
		#endregion

		#region 构造函数
		protected FixedElement(Type type, string name, Plugin plugin, FixedElementType elementType) : base(name, plugin)
		{
			if(plugin == null)
				throw new ArgumentNullException("plugin");
			if(type == null)
				throw new ArgumentNullException("type");

			_syncRoot = new object();
			_type = type;
			_fixedElementType = elementType;
		}

		protected FixedElement(string typeName, string name, Plugin plugin, FixedElementType elementType) : base(name, plugin)
		{
			if(plugin == null)
				throw new ArgumentNullException("plugin");
			if(string.IsNullOrWhiteSpace(typeName))
				throw new ArgumentNullException("typeName");

			_syncRoot = new object();
			_typeName = typeName;
			_fixedElementType = elementType;
		}
		#endregion

		#region 公共属性
		public Type Type
		{
			get
			{
				if(_type == null)
				{
					lock(_syncRoot)
					{
						if(_type == null)
						{
							Type type = PluginUtility.GetType(_typeName);

							if(!this.ValidateType(type))
								throw new InvalidOperationException();

							_type = type;
						}
					}
				}

				return _type;
			}
		}

		public FixedElementType FixedElementType
		{
			get
			{
				return _fixedElementType;
			}
		}

		public bool HasValue
		{
			get
			{
				return _value != null;
			}
		}
		#endregion

		#region 保护方法
		internal protected object GetValue()
		{
			if(_value == null)
			{
				lock(_syncRoot)
				{
					if(_value == null)
						_value = this.CreateValue();
				}
			}

			return _value;
		}
		#endregion

		#region 虚拟方法
		protected virtual bool ValidateType(Type type)
		{
			return type != null;
		}

		protected virtual object CreateValue()
		{
			if(this.Type == null)
				return null;

			try
			{
				object result = PluginUtility.BuildType(this.Type, (Type parameterType, string parameterName, out object parameterValue) =>
				{
					return PluginUtility.ObtainParameter(this.Plugin, parameterType, parameterName, out parameterValue);
				});

				if(result == null)
					throw new PluginException(string.Format("Can not build instance of '{0}' type, Maybe that's cause type-generator not found matched constructor with parameters. in '{1}' plugin.", this.Type.FullName, this.Plugin));

				return result;
			}
			catch(Exception ex)
			{
				throw new PluginException(string.Format("Occurred an exception on create a fixed-element instance of '{0}' type, at '{1}' plugin.", this.Type.FullName, this.Plugin), ex);
			}
		}
		#endregion
	}
}
