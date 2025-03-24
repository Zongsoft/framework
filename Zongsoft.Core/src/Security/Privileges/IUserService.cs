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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供用户服务的接口。
/// </summary>
public partial interface IUserService<TUser> where TUser : IUser
{
	/// <summary>获取用户密码器。</summary>
	Passworder Passworder { get; }

	/// <summary>确定指定的用户是否存在。</summary>
	/// <param name="identifier">指定要查找的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果指定的用户是存在的则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> ExistsAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>获取指定的用户对象。</summary>
	/// <param name="identifier">要查找的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的用户对象，如果没有找到则返回空(<c>null</c>)。</returns>
	ValueTask<TUser> GetAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>获取指定的用户对象。</summary>
	/// <param name="identifier">要查找的用户标识。</param>
	/// <param name="schema">获取的数据模式。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的用户对象，如果没有找到则返回空(<c>null</c>)。</returns>
	ValueTask<TUser> GetAsync(Identifier identifier, string schema, CancellationToken cancellation = default);

	/// <summary>查找指定关键字的用户。</summary>
	/// <param name="keyword">指定的查找关键字。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的用户结果集。</returns>
	IAsyncEnumerable<TUser> FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>查找指定条件的用户。</summary>
	/// <param name="criteria">指定的查找条件。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的用户结果集。</returns>
	IAsyncEnumerable<TUser> FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>更改用户名称。</summary>
	/// <param name="identifier">要更名的用户标识。</param>
	/// <param name="name">要更名的新名称。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更名成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> RenameAsync(Identifier identifier, string name, CancellationToken cancellation = default);

	/// <summary>设置指定用户的邮箱地址。</summary>
	/// <param name="identifier">要设置的用户标识。</param>
	/// <param name="email">要设置的邮箱地址。</param>
	/// <param name="verifiable">指定一个值，指示是否必须对设置的邮箱地址进行验证。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果设置成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> SetEmailAsync(Identifier identifier, string email, bool verifiable = true, CancellationToken cancellation = default);

	/// <summary>设置指定用户的手机号码。</summary>
	/// <param name="identifier">要设置的用户标识。</param>
	/// <param name="phone">要设置的手机号码。</param>
	/// <param name="verifiable">指定一个值，指示是否必须对设置的手机号码进行验证。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果设置成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> SetPhoneAsync(Identifier identifier, string phone, bool verifiable = true, CancellationToken cancellation = default);

	/// <summary>创建一个用户。</summary>
	/// <param name="user">要创建的用户对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果创建成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CreateAsync(TUser user, CancellationToken cancellation = default);

	/// <summary>创建一个用户，并为其设置密码。</summary>
	/// <param name="user">要创建的用户对象。</param>
	/// <param name="password">新建用户的初始密码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果创建成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CreateAsync(TUser user, string password, CancellationToken cancellation = default);

	/// <summary>创建多个用户。</summary>
	/// <param name="users">要创建的用户对象集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回创建成功的用户数量。</returns>
	ValueTask<int> CreateAsync(IEnumerable<TUser> users, CancellationToken cancellation = default);

	/// <summary>删除一个用户。</summary>
	/// <param name="identifier">要删除的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果删除成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> DeleteAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>删除多个用户。</summary>
	/// <param name="identifiers">要删除的用户标识集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回删除成功的用户数量。</returns>
	ValueTask<int> DeleteAsync(IEnumerable<Identifier> identifiers, CancellationToken cancellation = default);

	/// <summary>更新用户信息。</summary>
	/// <param name="user">要更新的用户对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更新成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> UpdateAsync(TUser user, CancellationToken cancellation = default);
}

partial interface IUserService<TUser>
{
	/// <summary>判断指定编号的用户是否设置了密码。</summary>
	/// <param name="identifier">指定要查找的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果返回真(True)表示指定编号的用户已经设置了密码，否则未设置密码。</returns>
	ValueTask<bool> HasPasswordAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>修改指定用户的密码。</summary>
	/// <param name="identifier">指定要查找的用户标识。</param>
	/// <param name="oldPassword">指定的用户的当前密码。</param>
	/// <param name="newPassword">指定的用户的新密码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果修改成功返回真(True)，否则返回假(False)。</returns>
	ValueTask<bool> ChangePasswordAsync(Identifier identifier, string oldPassword, string newPassword, CancellationToken cancellation = default);

	/// <summary>准备重置指定用户的密码。</summary>
	/// <param name="identity">要重置密码的用户标识，仅限用户的“邮箱地址”或“手机号码”。</param>
	/// <param name="namespace">指定的用户标识所属的命名空间。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回忘记密码重置的令牌，如果指定用户标识不存在则返回空(<c>null</c>)。</returns>
	ValueTask<string> ForgetPasswordAsync(string identity, string @namespace, CancellationToken cancellation = default);

	/// <summary>重置指定用户的密码，以验证码摘要的方式进行密码重置。</summary>
	/// <param name="token">要重置的令牌。</param>
	/// <param name="secret">重置密码的验证码。</param>
	/// <param name="password">重置后的新密码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果密码重置成功则返回真(True)，否则返回假(False)。</returns>
	/// <remarks>
	/// 	<para>本重置方法通常由Web请求的方式进行，请求的URL大致如下：
	/// 	<c>https://api.zongsoft.com/security/users/password/reset/[token]?secret=xxxxxx</c>
	/// 	</para>
	/// </remarks>
	ValueTask<bool> ResetPasswordAsync(string token, string secret, string password = null, CancellationToken cancellation = default);

	/// <summary>重置指定用户的密码，以密码问答的方式进行密码重置。</summary>
	/// <param name="identity">要重置的用户标识，可以是“用户名”、“邮箱地址”或“手机号码”。</param>
	/// <param name="namespace">指定的用户标识所属的命名空间。</param>
	/// <param name="passwordAnswers">指定用户的密码问答的答案集。</param>
	/// <param name="newPassword">重置后的新密码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果密码重置成功则返回真(True)，否则返回假(False)。</returns>
	/// <exception cref="SecurityException">如果指定的用户没有设置密码问答或者密码问答验证失败。</exception>
	ValueTask<bool> ResetPasswordAsync(string identity, string @namespace, string[] passwordAnswers, string newPassword = null, CancellationToken cancellation = default);

	/// <summary>获取指定用户的密码问答的题面集。</summary>
	/// <param name="identifier">指定要查找的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定用户的密码问答的题面，即密码问答的提示部分。</returns>
	ValueTask<string[]> GetPasswordQuestionsAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>获取指定用户的密码问答的题面集。</summary>
	/// <param name="identity">指定的用户标识，可以是“用户名”、“邮箱地址”或“手机号码”。</param>
	/// <param name="namespace">指定的用户标识所属的命名空间。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定用户的密码问答的题面，即密码问答的提示部分。</returns>
	ValueTask<string[]> GetPasswordQuestionsAsync(string identity, string @namespace, CancellationToken cancellation = default);

	/// <summary>设置指定用户的密码问答集。</summary>
	/// <param name="identifier">指定要查找的用户标识。</param>
	/// <param name="password">当前用户的密码，如果密码错误则无法更新密码问答。</param>
	/// <param name="passwordQuestions">当前用户的密码问答的题面集。</param>
	/// <param name="passwordAnswers">当前用户的密码问答的答案集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果设置成则返回真(True)，否则返回假(False)。</returns>
	ValueTask<bool> SetPasswordQuestionsAndAnswersAsync(Identifier identifier, string password, string[] passwordQuestions, string[] passwordAnswers, CancellationToken cancellation = default);
}