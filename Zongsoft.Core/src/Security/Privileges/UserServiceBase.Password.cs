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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

partial class UserServiceBase<TUser>
{
	#region 密码操作
	public ValueTask<bool> HasPasswordAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		return this.Accessor.ExistsAsync(this.Name, this.GetCriteria(identifier) & Condition.NotEqual("Password", null));
	}

	public ValueTask<bool> ChangePasswordAsync(Identifier identifier, string oldPassword, string newPassword, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<string> ForgetPasswordAsync(string identity, string @namespace, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<bool> ResetPasswordAsync(string token, string secret, string password = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<bool> ResetPasswordAsync(string identity, string @namespace, string[] passwordAnswers, string newPassword = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<string[]> GetPasswordQuestionsAsync(Identifier identifier, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<string[]> GetPasswordQuestionsAsync(string identity, string @namespace, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<bool> SetPasswordQuestionsAndAnswersAsync(Identifier identifier, string password, string[] passwordQuestions, string[] passwordAnswers, CancellationToken cancellation = default) => throw new NotImplementedException();
	#endregion

	#region 嵌套子类
	public class PassworderBase<TCipher>(UserServiceBase<TUser> service) : Passworder where TCipher : Passworder.Cipher
	{
		protected UserServiceBase<TUser> Service { get; } = service ?? throw new ArgumentNullException(nameof(service));

		public override async ValueTask<Cipher> GetAsync(string identity, string @namespace, CancellationToken cancellation)
		{
			var result = this.OnGetAsync(identity, @namespace, cancellation);
			await using var enumerator = result.GetAsyncEnumerator(cancellation);
			return await enumerator.MoveNextAsync() ? enumerator.Current : null;
		}

		public override ValueTask<bool> VerifyAsync(string password, Cipher cipher, CancellationToken cancellation)
		{
			return this.OnVerifyAsync(password, cipher as TCipher, cancellation);
		}

		protected virtual IAsyncEnumerable<TCipher> OnGetAsync(string identity, string @namespace, CancellationToken cancellation) =>
			this.Service.Accessor.SelectAsync<TCipher>(this.Service.Name, this.Service.GetCriteria(identity, @namespace), cancellation);
		protected virtual ValueTask<bool> OnVerifyAsync(string password, TCipher cipher, CancellationToken cancellation) =>
			ValueTask.FromResult(PasswordUtility.VerifyPassword(password, cipher.Value, cipher.Nonce, cipher.Name));
	}
	#endregion
}
