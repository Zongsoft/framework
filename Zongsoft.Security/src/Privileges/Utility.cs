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

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

internal static class Utility
{
	public static bool IsUser<T>(this ref Identifier identifier, out T id) where T : IEquatable<T>
	{
		return identifier.TryGetValue<T>(Users.Identifiers.User, out id);
	}

	public static bool IsRole<T>(this ref Identifier identifier, out T id) where T : IEquatable<T>
	{
		return identifier.TryGetValue<T>(Roles.Identifiers.Role, out id);
	}
}
