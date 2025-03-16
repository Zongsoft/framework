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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

[Service<IUserService<IUser>, IUserService<UserModel>>]
public class UserService : UserServiceBase<UserModel>, IUserService<IUser>
{
	#region 重写方法
	protected override IDataAccess Accessor => Module.Current.Accessor;
	protected override ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.Validate(out uint userId))
			return Condition.Equal(nameof(UserModel.UserId), userId);

		return base.GetCriteria(identifier);
	}
	#endregion

	#region 显式实现
	async ValueTask<IUser> IUserService<IUser>.GetAsync(Identifier identifier, CancellationToken cancellation) => await this.GetAsync(identifier, cancellation);
	async ValueTask<IUser> IUserService<IUser>.GetAsync(Identifier identifier, string schema, CancellationToken cancellation) => await this.GetAsync(identifier, schema, cancellation);
	IAsyncEnumerable<IUser> IUserService<IUser>.FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(keyword, schema, paging, cancellation);
	IAsyncEnumerable<IUser> IUserService<IUser>.FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(criteria, schema, paging, cancellation);
	ValueTask<bool> IUserService<IUser>.CreateAsync(IUser user, CancellationToken cancellation) => this.CreateAsync(user as UserModel, cancellation);
	ValueTask<bool> IUserService<IUser>.CreateAsync(IUser user, string password, CancellationToken cancellation) => this.CreateAsync(user as UserModel, password, cancellation);
	ValueTask<int> IUserService<IUser>.CreateAsync(IEnumerable<IUser> users, CancellationToken cancellation) => this.CreateAsync(users.Cast<UserModel>(), cancellation);
	ValueTask<bool> IUserService<IUser>.UpdateAsync(IUser user, CancellationToken cancellation) => this.UpdateAsync(user as UserModel, cancellation);
	#endregion
}
