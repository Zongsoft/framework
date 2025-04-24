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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

internal class Utility
{
	public static NodeId GetDataType(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		if(Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType))
			type = underlyingType;

		if(type.IsEnum)
			return DataTypeIds.Enumeration;

		if(type == typeof(Guid))
			return DataTypeIds.Guid;

		return Type.GetTypeCode(type) switch
		{
			TypeCode.Boolean => DataTypeIds.Boolean,
			TypeCode.Byte => DataTypeIds.Byte,
			TypeCode.SByte => DataTypeIds.SByte,
			TypeCode.Int16 => DataTypeIds.Int16,
			TypeCode.Int32 => DataTypeIds.Int32,
			TypeCode.Int64 => DataTypeIds.Int64,
			TypeCode.UInt16 => DataTypeIds.UInt16,
			TypeCode.UInt32 => DataTypeIds.UInt32,
			TypeCode.UInt64 => DataTypeIds.UInt64,
			TypeCode.Single => DataTypeIds.Float,
			TypeCode.Double => DataTypeIds.Double,
			TypeCode.Decimal => DataTypeIds.Decimal,
			TypeCode.DateTime => DataTypeIds.DateTime,
			TypeCode.String => DataTypeIds.String,
			TypeCode.DBNull => null,
			TypeCode.Object => DataTypeIds.ObjectTypeNode,
			_ => DataTypeIds.ObjectTypeNode,
		};
	}
}
