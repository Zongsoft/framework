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
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Security.Privileges.Models;

[Model($"{Module.NAME}.Privilege")]
public abstract class PrivilegeModel
{
	public abstract uint MemberId { get; set; }
	public abstract MemberType MemberType { get; set; }
	public abstract string PrivilegeName { get; set; }
	public abstract PrivilegeMode PrivilegeMode { get; set; }
}

[Model($"{Module.NAME}.PrivilegeFiltering")]
public abstract class PrivilegeFilteringModel
{
	public abstract uint MemberId { get; set; }
	public abstract MemberType MemberType { get; set; }
	public abstract string PrivilegeName { get; set; }
	public abstract string PrivilegeFilter { get; set; }
}
