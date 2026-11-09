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

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public interface IMember : IEquatable<IMember>
{
	Identifier RoleId { get; }
	Identifier MemberId { get; }
	MemberType MemberType { get; }

	object Member { get; }
}

public interface IMember<TRole> : IMember, IEquatable<IMember<TRole>> where TRole : IRole
{
	TRole Role { get; }
}
