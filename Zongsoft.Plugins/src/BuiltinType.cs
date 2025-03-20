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
	public class BuiltinType
	{
		#region 成员变量
		private Type _type;
		private Builtin _builtin;
		private string _typeName;
		private BuiltinTypeConstructor _constructor;
		#endregion

		#region 构造函数
		public BuiltinType(Builtin builtin, string typeName)
		{
			if(string.IsNullOrWhiteSpace(typeName))
				throw new ArgumentNullException(nameof(typeName));

			_type = null;
			_builtin = builtin ?? throw new ArgumentNullException(nameof(builtin));
			_typeName = typeName.Trim();
			_constructor = new BuiltinTypeConstructor(this);
		}
		#endregion

		#region 公共属性
		public Builtin Builtin => _builtin;
		public BuiltinTypeConstructor Constructor => _constructor;
		public Type Type => _type ??= this.Discriminate() ?? PluginUtility.GetType(this.TypeName, _builtin);
		public string TypeName
		{
			get => _typeName;
			internal set
			{
				_typeName = value?.Trim();
				_type = null;
			}
		}
		#endregion

		#region 鉴定类型
		private Type Discriminate()
		{
			var ownerNode = PluginUtility.GetOwnerNode(_builtin);
			var ownerValue = ownerNode?.UnwrapValue(ObtainMode.Never);

			if(ownerValue == null)
				return null;

			if(ownerValue is Components.IDiscriminator ownerDiscriminator)
				return this.Discriminate(ownerDiscriminator);

			var memberValue = PluginUtility.GetDefaultMemberValue(ownerValue);
			if(memberValue is Components.IDiscriminator memberDiscriminator)
				return this.Discriminate(memberDiscriminator);

			return null;
		}

		private Type Discriminate(Components.IDiscriminator discriminator)
		{
			if(discriminator == null)
				return null;

			var result = discriminator.Discriminate(_typeName);

			if(result == null)
				return null;

			if(result is Type type)
				return type;

			return PluginUtility.GetImplementedCollectionElementType(result.GetType());
		}
		#endregion
	}
}
