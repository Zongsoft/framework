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

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

public partial class RoleService : RoleServiceBase<RoleModel>
{
	#region 重写方法
	protected override IDataAccess Accessor => Module.Current.Accessor;
	protected override ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.TryGetValue<uint>(Roles.Identifiers.Role, out var roleId))
			return Condition.Equal(nameof(RoleModel.RoleId), roleId);

		return base.GetCriteria(identifier);
	}
	#endregion
}