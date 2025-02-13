﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 提供关于用户管理的接口。
	/// </summary>
	public interface IUserProvider<TUser> where TUser : IUserModel
	{
		#region 事件定义
		/// <summary>表示用户信息发生更改之后的事件。</summary>
		event EventHandler<ChangedEventArgs> Changed;
		#endregion

		#region 用户管理
		/// <summary>获取指定编号对应的用户对象。</summary>
		/// <param name="userId">要查找的用户编号。</param>
		/// <returns>返回由<paramref name="userId"/>参数指定的用户对象，如果没有找到指定编号的用户则返回空。</returns>
		TUser GetUser(uint userId);

		/// <summary>获取指定标识对应的用户对象。</summary>
		/// <param name="identity">要查找的用户标识，可以是“用户名称”、“绑定的电子邮箱”或“绑定的手机号码”。</param>
		/// <param name="namespace">要查找的用户标识所属的命名空间，如果为空(null)或空字符串("")则表示当前用户所在命名空间。</param>
		/// <returns>返回找到的用户对象；如果在指定的命名空间内没有找到指定标识的用户则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="identity"/>参数为空(null)或者全空格字符。</exception>
		TUser GetUser(string identity, string @namespace = null);

		/// <summary>获取指定命名空间中的用户集。</summary>
		/// <param name="namespace">要获取的用户集所属的命名空间。如果为星号(*)则忽略命名空间即系统中的所有用户；如果为空(null)或空字符串("")则查找当前用户所在命名空间的用户集。</param>
		/// <param name="paging">查询的分页设置，默认为第一页。</param>
		/// <returns>返回当前命名空间中的所有用户对象集。</returns>
		IEnumerable<TUser> GetUsers(string @namespace, Zongsoft.Data.Paging paging = null);

		/// <summary>查找指定关键字的用户。</summary>
		/// <param name="keyword">要查找的关键字。</param>
		/// <param name="paging">查询的分页设置，默认为第一页。</param>
		/// <returns>返回找到的用户对象集。</returns>
		IEnumerable<TUser> Find(string keyword, Zongsoft.Data.Paging paging = null);

		/// <summary>确定指定编号的用户是否存在。</summary>
		/// <param name="userId">指定要查找的用户编号。</param>
		/// <returns>如果指定编号的用户是存在的则返回真(True)，否则返回假(False)。</returns>
		bool Exists(uint userId);

		/// <summary>确定指定的用户标识在指定的命名空间内是否已经存在。</summary>
		/// <param name="identity">要确定的用户标识，可以是“用户名”或“邮箱地址”或“手机号码”。</param>
		/// <param name="namespace">要确定的用户标识所属的命名空间，如果为空(null)或空字符串("")则表示当前用户所在命名空间。</param>
		/// <returns>如果指定的用户标识在命名空间内已经存在则返回真(True)，否则返回假(False)。</returns>
		bool Exists(string identity, string @namespace = null);

		/// <summary>设置指定编号的用户邮箱地址。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="email">要设置的邮箱地址。</param>
		/// <param name="verifiable">指定一个值，指示是否必须对设置的邮箱地址进行验证。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetEmail(uint userId, string email, bool verifiable = true);

		/// <summary>设置指定编号的用户手机号码。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="phone">要设置的手机号码。</param>
		/// <param name="verifiable">指定一个值，指示是否必须对设置的手机号码进行验证。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetPhone(uint userId, string phone, bool verifiable = true);

		/// <summary>设置指定编号的用户所属命名空间。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="namespace">要设置的命名空间。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假。</returns>
		bool SetNamespace(uint userId, string @namespace);

		/// <summary>更新指定命名空间下所有用户到新的命名空间。</summary>
		/// <param name="oldNamespace">指定的旧命名空间。</param>
		/// <param name="newNamespace">指定的新命名空间。</param>
		/// <returns>返回更新成功的用户数。</returns>
		int SetNamespaces(string oldNamespace, string newNamespace);

		/// <summary>设置指定编号的用户名称。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="name">要设置的用户名称。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetName(uint userId, string name);

		/// <summary>设置指定编号的用户昵称。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="nickname">要设置的用户昵称。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetNickname(uint userId, string nickname);

		/// <summary>设置指定编号的用户描述信息。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="description">要设置的用户描述信息。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetDescription(uint userId, string description);

		/// <summary>设置指定编号的用户状态。</summary>
		/// <param name="userId">要设置的用户编号。</param>
		/// <param name="status">指定的用户状态。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetStatus(uint userId, UserStatus status);

		/// <summary>删除指定编号集的多个用户。</summary>
		/// <param name="ids">要删除的用户编号数组。</param>
		/// <returns>如果删除成功则返回删除的数量，否则返回零。</returns>
		int Delete(params uint[] ids);

		/// <summary>创建一个用户。</summary>
		/// <param name="identity">要创建的用户标识（用户名、手机号、邮箱地址）。</param>
		/// <param name="namespace">新建用户所属的命名空间。</param>
		/// <param name="status">指定的新建用户的状态。</param>
		/// <param name="description">指定的新建用户的描述信息。</param>
		/// <returns>返回创建成功的用户对象，如果为空(null)则表示创建失败。</returns>
		TUser Create(string identity, string @namespace, UserStatus status = UserStatus.Active, string description = null);

		/// <summary>创建一个用户。</summary>
		/// <param name="identity">要创建的用户标识（用户名、手机号、邮箱地址）。</param>
		/// <param name="namespace">新建用户所属的命名空间。</param>
		/// <param name="password">为新创建用户的设置的密码。</param>
		/// <param name="status">指定的新建用户的状态。</param>
		/// <param name="description">指定的新建用户的描述信息。</param>
		/// <returns>返回创建成功的用户对象，如果为空(null)则表示创建失败。</returns>
		TUser Create(string identity, string @namespace, string password, UserStatus status = UserStatus.Active, string description = null);

		/// <summary>创建一个用户，并为其设置密码。</summary>
		/// <param name="user">要创建的<seealso cref="IUserModel"/>用户对象。</param>
		/// <param name="password">为新创建用户的设置的密码。</param>
		/// <returns>如果创建成功则返回真(true)，否则返回假(false)。</returns>
		bool Create(TUser user, string password = null);

		/// <summary>创建单个或者多个用户。</summary>
		/// <param name="users">要创建的用户对象集。</param>
		/// <returns>返回创建成功的用户数量。</returns>
		int Create(IEnumerable<TUser> users);

		/// <summary>注册一个用户。</summary>
		/// <param name="namespace">注册用户所属的命名空间。</param>
		/// <param name="identity">注册的用户标识。</param>
		/// <param name="token">注册的安全标记。</param>
		/// <param name="password">指定的用户密码。</param>
		/// <param name="parameters">指定的其他参数集。</param>
		/// <returns>返回注册成功的用户对象，如果为空(null)则表示注册失败。</returns>
		TUser Register(string @namespace, string identity, string token, string password, IDictionary<string, object> parameters = null);

		/// <summary>修改指定编号的用户信息。</summary>
		/// <param name="userId">指定要修改的用户编号。</param>
		/// <param name="user">要修改的用户对象。</param>
		/// <returns>如果修改成功则返回真(true)，否则返回假(false)。</returns>
		bool Update(uint userId, TUser user);
		#endregion

		#region 密码管理
		/// <summary>判断指定编号的用户是否设置了密码。</summary>
		/// <param name="userId">指定的用户编号。</param>
		/// <returns>如果返回真(True)表示指定编号的用户已经设置了密码，否则未设置密码。</returns>
		bool HasPassword(uint userId);

		/// <summary>判断指定标识的用户是否设置了密码。</summary>
		/// <param name="identity">指定的用户标识。</param>
		/// <param name="namespace">指定的用户标识所属的命名空间。</param>
		/// <returns>如果返回真(True)表示指定标识的用户已经设置了密码，否则未设置密码。</returns>
		bool HasPassword(string identity, string @namespace);

		/// <summary>修改指定用户的密码。</summary>
		/// <param name="userId">要修改密码的用户编号。</param>
		/// <param name="oldPassword">指定的用户的当前密码。</param>
		/// <param name="newPassword">指定的用户的新密码。</param>
		/// <returns>如果修改成功返回真(True)，否则返回假(False)。</returns>
		bool ChangePassword(uint userId, string oldPassword, string newPassword);

		/// <summary>准备重置指定用户的密码。</summary>
		/// <param name="identity">要重置密码的用户标识，仅限用户的“邮箱地址”或“手机号码”。</param>
		/// <param name="namespace">指定的用户标识所属的命名空间。</param>
		/// <returns>返回忘记密码重置的令牌，如果指定用户标识不存在则返回空(<c>null</c>)。</returns>
		string ForgetPassword(string identity, string @namespace);

		/// <summary>重置指定用户的密码，以验证码摘要的方式进行密码重置。</summary>
		/// <param name="token">要重置的令牌。</param>
		/// <param name="secret">重置密码的验证码。</param>
		/// <param name="password">重置后的新密码。</param>
		/// <returns>如果密码重置成功则返回真(True)，否则返回假(False)。</returns>
		/// <remarks>
		/// 	<para>本重置方法通常由Web请求的方式进行，请求的URL大致如下：
		/// 	<c>https://api.zongsoft.com/security/users/password/reset/[token]?secret=xxxxxx</c>
		/// 	</para>
		/// </remarks>
		bool ResetPassword(string token, string secret, string password = null);

		/// <summary>重置指定用户的密码，以密码问答的方式进行密码重置。</summary>
		/// <param name="identity">要重置的用户标识，可以是“用户名”、“邮箱地址”或“手机号码”。</param>
		/// <param name="namespace">指定的用户标识所属的命名空间。</param>
		/// <param name="passwordAnswers">指定用户的密码问答的答案集。</param>
		/// <param name="newPassword">重置后的新密码。</param>
		/// <returns>如果密码重置成功则返回真(True)，否则返回假(False)。</returns>
		/// <exception cref="SecurityException">如果指定的用户没有设置密码问答或者密码问答验证失败。</exception>
		bool ResetPassword(string identity, string @namespace, string[] passwordAnswers, string newPassword = null);

		/// <summary>获取指定用户的密码问答的题面集。</summary>
		/// <param name="userId">指定的用户编号。</param>
		/// <returns>返回指定用户的密码问答的题面，即密码问答的提示部分。</returns>
		string[] GetPasswordQuestions(uint userId);

		/// <summary>获取指定用户的密码问答的题面集。</summary>
		/// <param name="identity">指定的用户标识，可以是“用户名”、“邮箱地址”或“手机号码”。</param>
		/// <param name="namespace">指定的用户标识所属的命名空间。</param>
		/// <returns>返回指定用户的密码问答的题面，即密码问答的提示部分。</returns>
		string[] GetPasswordQuestions(string identity, string @namespace);

		/// <summary>设置指定用户的密码问答集。</summary>
		/// <param name="userId">要设置密码问答集的用户编号。</param>
		/// <param name="password">当前用户的密码，如果密码错误则无法更新密码问答。</param>
		/// <param name="passwordQuestions">当前用户的密码问答的题面集。</param>
		/// <param name="passwordAnswers">当前用户的密码问答的答案集。</param>
		/// <returns>如果设置成则返回真(True)，否则返回假(False)。</returns>
		bool SetPasswordQuestionsAndAnswers(uint userId, string password, string[] passwordQuestions, string[] passwordAnswers);
		#endregion

		#region 秘密校验
		/// <summary>校验指定的秘钥是否正确。</summary>
		/// <param name="userId">指定的用户编号。</param>
		/// <param name="type">指定的待校验的类型名。</param>
		/// <param name="secret">指定的待校验的秘钥。</param>
		/// <returns>如果校验成功则返回真(True)，否则返回假(False)。</returns>
		bool Verify(uint userId, string type, string secret);
		#endregion
	}
}
