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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

/// <summary>表示节点种类的枚举。</summary>
[Flags]
public enum OpcNodeKind : byte
{
	/// <summary>未定义</summary>
	None,
	/// <summary>对象</summary>
	Object = 1,
	/// <summary>变量</summary>
	Variable = 2,
	/// <summary>方法</summary>
	Method = 4,
	/// <summary>对象类型</summary>
	ObjectType = 8,
	/// <summary>变量类型</summary>
	VariableType = 16,
	/// <summary>引用类型</summary>
	ReferenceType = 32,
	/// <summary>数据类型</summary>
	DataType = 64,
	/// <summary>视图</summary>
	View = 128,
}

internal static class OpcNodeKindExtension
{
	public static OpcNodeKind ToKind(this NodeClass @class)
	{
		var kind = OpcNodeKind.None;

		if((@class & NodeClass.Object) == NodeClass.Object)
			kind |= OpcNodeKind.Object;
		if((@class & NodeClass.Variable) == NodeClass.Variable)
			kind |= OpcNodeKind.Variable;
		if((@class & NodeClass.Method) == NodeClass.Method)
			kind |= OpcNodeKind.Method;
		if((@class & NodeClass.ObjectType) == NodeClass.ObjectType)
			kind |= OpcNodeKind.ObjectType;
		if((@class & NodeClass.VariableType) == NodeClass.VariableType)
			kind |= OpcNodeKind.VariableType;
		if((@class & NodeClass.ReferenceType) == NodeClass.ReferenceType)
			kind |= OpcNodeKind.ReferenceType;
		if((@class & NodeClass.DataType) == NodeClass.DataType)
			kind |= OpcNodeKind.DataType;
		if((@class & NodeClass.View) == NodeClass.View)
			kind |= OpcNodeKind.View;

		return kind;
	}

	public static NodeClass ToClass(this OpcNodeKind kind)
	{
		var @class = NodeClass.Unspecified;

		if((kind & OpcNodeKind.Object) == OpcNodeKind.Object)
			@class |= NodeClass.Object;
		if((kind & OpcNodeKind.Variable) == OpcNodeKind.Variable)
			@class |= NodeClass.Variable;
		if((kind & OpcNodeKind.Method) == OpcNodeKind.Method)
			@class |= NodeClass.Method;
		if((kind & OpcNodeKind.ObjectType) == OpcNodeKind.ObjectType)
			@class |= NodeClass.ObjectType;
		if((kind & OpcNodeKind.VariableType) == OpcNodeKind.VariableType)
			@class |= NodeClass.VariableType;
		if((kind & OpcNodeKind.ReferenceType) == OpcNodeKind.ReferenceType)
			@class |= NodeClass.ReferenceType;
		if((kind & OpcNodeKind.DataType) == OpcNodeKind.DataType)
			@class |= NodeClass.DataType;
		if((kind & OpcNodeKind.View) == OpcNodeKind.View)
			@class |= NodeClass.View;

		return @class;
	}
}