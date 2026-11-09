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

namespace Zongsoft.Security;

public static class ClaimNames
{
	private const string URI = "http://schemas.zongsoft.com/security/claims";

	public const string Namespace     = $"{URI}/namespace";
	public const string Description   = $"{URI}/description";
	public const string Creation      = $"{URI}/creation";
	public const string Modification  = $"{URI}/modification";
	public const string Authorization = $"{URI}/authorization";
}
