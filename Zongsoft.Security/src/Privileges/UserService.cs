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
using System.Security.Claims;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

public partial class UserService : UserServiceBase<UserModel>
{
	#region 构造函数
	public UserService() => base.Passworder = new Passworder(this);
	#endregion

	#region 重写方法
	protected override IDataAccess Accessor => Module.Current.Accessor;
	protected override IServiceProvider Services => Module.Current.Services;
	protected override ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.Validate<IUser, uint>(out var userId))
			return Condition.Equal(nameof(UserModel.UserId), userId);

		return base.GetCriteria(identifier);
	}

	protected override ValueTask<Identifier> EnsureIdentityAsync(Identifier identifier, CancellationToken cancellation)
	{
		var identity = this.Principal?.Identity as ClaimsIdentity;
		var identityId = identity?.GetIdentifier()?.ToString();

		if(identity == null || !identity.IsAuthenticated || string.IsNullOrEmpty(identityId))
			throw new AuthorizationException(SecurityReasons.InvalidIdentity, Properties.Resources.Authorization_Unauthenticated_Message);

		if(identifier.IsEmpty)
		{
			object current = uint.TryParse(identityId, out var number) ? number : identityId;
			identifier = new Identifier(typeof(IUser), current);
		}

		if(!identifier.Validate<IUser>())
			throw Zongsoft.Common.OperationException.Argument();

		if(string.Equals(identityId, identifier.Value?.ToString(), StringComparison.Ordinal) || identity.InRoles([IRole.Administrators, IRole.Security]))
			return ValueTask.FromResult(identifier);

		throw new AuthorizationException(SecurityReasons.Forbidden, Properties.Resources.UserService_Unauthorized_Message);
	}
	#endregion
}

partial class UserService
{
	public new class Passworder(UserService service) : Zongsoft.Security.Privileges.Passworder
	{
		#region 成员字段
		private readonly UserService _service = service ?? throw new ArgumentNullException(nameof(service));
		#endregion

		#region 公共方法
		public override async ValueTask<bool> SetAsync(Zongsoft.Components.Identifier identifier, string password, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(identifier.IsEmpty)
				return false;

			return await _service.Accessor.UpdateAsync(_service.Name, new
			{
				Password = (byte[])Zongsoft.Security.Password.Generate(password),
			}, _service.GetCriteria(identifier), cancellation) > 0;
		}

		public override async ValueTask<bool> VerifyAsync(Zongsoft.Components.Identifier identifier, string password, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(identifier.IsEmpty)
				return false;

			var record = await this.GetAsync(_service.GetCriteria(identifier), cancellation);
			return record.HasValue && Verify(password, record.Value);
		}

		public override async ValueTask<Zongsoft.Components.Identifier> VerifyAsync(string identity, string @namespace, string password, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(string.IsNullOrWhiteSpace(identity))
				throw new AuthenticationException(SecurityReasons.InvalidIdentity);

			var record = await this.GetAsync(_service.GetCriteria(identity, @namespace), cancellation);
			if(!record.HasValue)
				throw new AuthenticationException(SecurityReasons.InvalidIdentity);

			return Verify(password, record.Value) ? new(typeof(IUser), record.Value.UserId) : default;
		}
		#endregion

		#region 私有方法
		private async ValueTask<PasswordRecord?> GetAsync(ICondition criteria, CancellationToken cancellation)
		{
			var records = _service.Accessor.SelectAsync<PasswordRecord?>(_service.Name, criteria, cancellation);
			await using var enumerator = records.GetAsyncEnumerator(cancellation);
			return await enumerator.MoveNextAsync() ? enumerator.Current : null;
		}

		private static bool Verify(string password, PasswordRecord record)
		{
			if(!record.Enabled)
				throw new AuthenticationException(SecurityReasons.AccountDisabled);

			return Zongsoft.Security.Password.TryParse(record.Password, out var stored) && stored.Verify(password);
		}
		#endregion

		#region 嵌套结构
		private struct PasswordRecord
		{
			public uint UserId { get; set; }
			public bool Enabled { get; set; }
			public byte[] Password { get; set; }
		}
		#endregion
	}
}
