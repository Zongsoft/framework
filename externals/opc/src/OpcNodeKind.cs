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

/// <summary>
/// 表示节点种类的枚举。
/// </summary>
public enum OpcNodeKind : byte
{
	/// <summary>未定义</summary>
	None,
	/// <summary>对象</summary>
	Object,
	/// <summary>变量</summary>
	Variable,
	/// <summary>方法</summary>
	Method,
	/// <summary>对象类型</summary>
	ObjectType,
	/// <summary>变量类型</summary>
	VariableType,
	/// <summary>引用类型</summary>
	ReferenceType,
	/// <summary>数据类型</summary>
	DataType,
	/// <summary>视图</summary>
	View,
}

internal static class OpcNodeKindExtension
{
	public static OpcNodeKind ToKind(this NodeClass @class) => @class switch
	{
		NodeClass.Object => OpcNodeKind.Object,
		NodeClass.Variable => OpcNodeKind.Variable,
		NodeClass.Method => OpcNodeKind.Method,
		NodeClass.ObjectType => OpcNodeKind.ObjectType,
		NodeClass.VariableType => OpcNodeKind.VariableType,
		NodeClass.ReferenceType => OpcNodeKind.ReferenceType,
		NodeClass.DataType => OpcNodeKind.DataType,
		NodeClass.View => OpcNodeKind.View,
		_ => OpcNodeKind.None,
	};

	public static NodeClass ToClass(this OpcNodeKind kind) => kind switch
	{
		OpcNodeKind.None => NodeClass.Unspecified,
		OpcNodeKind.Object => NodeClass.Object,
		OpcNodeKind.Variable => NodeClass.Variable,
		OpcNodeKind.Method => NodeClass.Method,
		OpcNodeKind.ObjectType => NodeClass.ObjectType,
		OpcNodeKind.VariableType => NodeClass.VariableType,
		OpcNodeKind.ReferenceType => NodeClass.ReferenceType,
		OpcNodeKind.DataType => NodeClass.DataType,
		OpcNodeKind.View => NodeClass.View,
		_ => NodeClass.Unspecified,
	};
}