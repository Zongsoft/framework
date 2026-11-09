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

namespace Zongsoft.Security.Privileges;

/// <summary>表示授权定义项的接口。</summary>
public interface IPrivilege : IEquatable<IPrivilege>
{
	/// <summary>获取权限名称。</summary>
	string Name { get; }

	/// <summary>获取一个值，指示是否为空的授权项。</summary>
	bool IsEmpty { get; }
}
