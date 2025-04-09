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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供安全凭证相关操作的功能。
	/// </summary>
	public interface ICredentialProvider
	{
		#region 事件定义
		/// <summary>表示注册完成事件。</summary>
		event EventHandler<CredentialRegisterEventArgs> Registered;

		/// <summary>表示准备注册事件。</summary>
		event EventHandler<CredentialRegisterEventArgs> Registering;

		/// <summary>表示注销完成事件。</summary>
		event EventHandler<CredentialUnregisterEventArgs> Unregistered;

		/// <summary>表示准备注销事件。</summary>
		event EventHandler<CredentialUnregisterEventArgs> Unregistering;
		#endregion

		#region 方法定义
		/// <summary>将指定的凭证主体注册到凭证容器中。</summary>
		/// <param name="principal">指定要注册的凭证主体对象。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		ValueTask RegisterAsync(CredentialPrincipal principal, CancellationToken cancellation = default);

		/// <summary>从安全凭证容器中注销指定的凭证。</summary>
		/// <param name="credentialId">指定的要注销的安全凭证编号。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		ValueTask UnregisterAsync(string credentialId, CancellationToken cancellation = default);

		/// <summary>续约指定的凭证主体。</summary>
		/// <param name="credentialId">指定要续约的安全凭证编号。</param>
		/// <param name="token">续约的安全标记。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回续约成功的凭证主体对象，如果续约失败则返回空(<c>null</c>)。</returns>
		/// <remarks>注：续约操作会依次激发“Unregistered”和“Registered”事件。</remarks>
		ValueTask<CredentialPrincipal> RenewAsync(string credentialId, string token, CancellationToken cancellation = default);

		/// <summary>刷新指定编号的凭证主体。</summary>
		/// <param name="credentialId">指定要刷新的安全凭证编号。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回刷新成功的凭证主体。</returns>
		ValueTask<CredentialPrincipal> RefreshAsync(string credentialId, CancellationToken cancellation = default);

		/// <summary>刷新指定标识及应用场景对应的凭证主体。</summary>
		/// <param name="identifier">指定要刷新的安全凭证对应的用户唯一标识，通常为用户主键。</param>
		/// <param name="scenario">指定要刷新的安全凭证对应的应用场景，为空<c>null</c>或星号表示不限场景。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回刷新成功的凭证主体集。</returns>
		IAsyncEnumerable<CredentialPrincipal> RefreshAsync(string identifier, string scenario, CancellationToken cancellation = default);

		/// <summary>获取指定安全凭证编号对应的凭证主体。</summary>
		/// <param name="credentialId">指定要获取的安全凭证编号。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的对应的凭证主体，如果指定的安全凭证编号不存在则返回空(<c>null</c>)。</returns>
		ValueTask<CredentialPrincipal> GetPrincipalAsync(string credentialId, CancellationToken cancellation = default);

		/// <summary>获取指定标识及应用场景对应的凭证主体集。</summary>
		/// <param name="identifier">指定要获取的安全凭证对应的用户唯一标识，通常为用户主键。</param>
		/// <param name="scenario">指定要获取的安全凭证对应的应用场景，为空<c>null</c>或星号表示不限场景。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回成功的凭证主体集。</returns>
		IAsyncEnumerable<CredentialPrincipal> GetPrincipalsAsync(string identifier, string scenario, CancellationToken cancellation = default);
		#endregion
	}
}
