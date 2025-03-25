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
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using System.Threading.Tasks;

namespace Zongsoft.Security.Membership
{
	public abstract class UserProviderBase<TUser> : IUserProvider<TUser> where TUser : IUserModel
	{
		#region 常量定义
		private const string KEY_EMAIL_SECRET = "user.email";
		private const string KEY_PHONE_SECRET = "user.phone";
		private const string KEY_FORGET_SECRET = "user.forget";

		private const string KEY_AUTHENTICATION_TEMPLATE = "Authentication";
		private const string KEY_IMPORTANT_CHANGE_TEMPLATE = "Important.Change";
		#endregion

		#region 事件定义
		public event EventHandler<ChangedEventArgs> Changed;
		#endregion

		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		protected UserProviderBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		[ServiceDependency]
		public ISecretor Secretor { get; set; }

		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
				{
					_dataAccess = this.ServiceProvider.ResolveRequired<IDataAccessProvider>().GetAccessor(MembershipUtility.Security);

					if(_dataAccess != null && !string.IsNullOrEmpty(Mapping.Instance.User))
					{
						_dataAccess.Naming.Map<TUser>(Mapping.Instance.User);
					}
				}
				return _dataAccess;
			}
		}

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 用户管理
		public TUser GetUser(uint userId)
		{
			return this.DataAccess.Select<TUser>(Condition.Equal(nameof(IUserModel.UserId), GetUserId(userId))).FirstOrDefault();
		}

		public TUser GetUser(string identity, string @namespace = null)
		{
			EnsureRoles();
			return this.DataAccess.Select<TUser>(MembershipUtility.GetIdentityCondition(identity) & this.GetNamespace(@namespace)).FirstOrDefault();
		}

		public IEnumerable<TUser> GetUsers(string @namespace, Paging paging = null)
		{
			EnsureRoles();
			return this.DataAccess.Select<TUser>(this.GetNamespace(@namespace), paging);
		}

		public virtual IEnumerable<TUser> Find(string keyword, Paging paging = null)
		{
			EnsureRoles();
			return this.DataAccess.Select<TUser>(Condition.Like(nameof(IUserModel.Name), keyword), paging);
		}

		public bool Exists(uint userId)
		{
			return this.DataAccess.Exists<TUser>(Condition.Equal(nameof(IUserModel.UserId), userId));
		}

		public bool Exists(string identity, string @namespace = null)
		{
			if(string.IsNullOrWhiteSpace(identity))
				return false;

			return this.DataAccess.Exists<TUser>(MembershipUtility.GetIdentityCondition(identity) & this.GetNamespace(@namespace));
		}

		public bool SetEmail(uint userId, string email, bool verifiable = true)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId, out var secured);

			//判断是否邮箱地址是否需要校验
			if(verifiable && !secured)
			{
				//获取指定编号的用户
				var user = this.GetUser(userId);

				if(user == null)
					return false;

				//发送邮箱地址更改的校验通知
				this.OnChangeEmail(user, email);

				//返回成功
				return true;
			}

			if(this.DataAccess.Update<TUser>(
				new
				{
					Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
					Modification = DateTime.Now,
				}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserModel.Email), email);
				return true;
			}

			return false;
		}

		public bool SetPhone(uint userId, string phone, bool verifiable = true)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId, out var secured);

			//判断是否电话号码是否需要校验
			if(verifiable && !secured)
			{
				//获取指定编号的用户
				var user = this.GetUser(userId);

				if(user == null)
					return false;

				//发送电话号码更改的校验通知
				this.OnChangePhone(user, phone);

				//返回成功
				return true;
			}

			if(this.DataAccess.Update<TUser>(
				new
				{
					Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
					Modification = DateTime.Now,
				}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserModel.Phone), phone);
				return true;
			}

			return false;
		}

		public bool SetNamespace(uint userId, string @namespace)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			if(this.DataAccess.Update<TUser>(
				new
				{
					Namespace = string.IsNullOrWhiteSpace(@namespace) ? null : @namespace.Trim(),
					Modification = DateTime.Now,
				},
				new Condition(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserModel.Namespace), @namespace);
				return true;
			}

			return false;
		}

		public int SetNamespaces(string oldNamespace, string newNamespace)
		{
			EnsureRoles();

			return this.DataAccess.Update<TUser>(
				new
				{
					Namespace = string.IsNullOrWhiteSpace(newNamespace) ? null : newNamespace.Trim(),
					Modification = DateTime.Now,
				},
				new Condition(nameof(IUserModel.Namespace), oldNamespace));
		}

		public bool SetName(uint userId, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			//验证指定的名称是否为系统内置名
			if(string.Equals(name, IUserModel.Administrator, StringComparison.OrdinalIgnoreCase))
				throw new SecurityException("username.illegality", "The user name specified to be update cannot be a built-in name.");

			//验证指定的名称是否合法
			this.OnValidateName(name);

			if(this.DataAccess.Update<TUser>(
				new
				{
					Name = name.Trim(),
					Modification = DateTime.Now,
				},
				new Condition(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserIdentity.Name), name);
				return true;
			}

			return false;
		}

		public bool SetNickname(uint userId, string nickname)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			if(this.DataAccess.Update<TUser>(
				new
				{
					Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim(),
					Modification = DateTime.Now,
				},
				new Condition(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserIdentity.Nickname), nickname);
				return true;
			}

			return false;
		}

		public bool SetStatus(uint userId, UserStatus status)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			var timestamp = DateTime.Now;

			if(this.DataAccess.Update<TUser>(
				new
				{
					Status = status,
					StatusTimestamp = timestamp,
					Modification = timestamp,
				},
				new Condition(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserModel.Status), status);
				return true;
			}

			return false;
		}

		public bool SetDescription(uint userId, string description)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			if(this.DataAccess.Update<TUser>(
				new
				{
					Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
					Modification = DateTime.Now,
				},
				new Condition(nameof(IUserModel.UserId), userId)) > 0)
			{
				this.OnChanged(userId, nameof(IUserModel.Description), description);
				return true;
			}

			return false;
		}

		public TUser Create(string identity, string @namespace, UserStatus status = UserStatus.Active, string description = null)
		{
			return this.Create(identity, @namespace, null, status, description);
		}

		public TUser Create(string identity, string @namespace, string password, UserStatus status = UserStatus.Active, string description = null)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			var user = this.CreateUser(null);

			user.Namespace = @namespace;
			user.Status = status;
			user.Description = string.IsNullOrEmpty(description) ? null : description;

			switch(UserIdentityTypeUtility.Detect(identity))
			{
				case UserIdentityType.Name:
					user.Name = identity;
					break;
				case UserIdentityType.Phone:
					user.Phone = identity;
					break;
				case UserIdentityType.Email:
					user.Email = identity;
					break;
			}

			return this.Create(user, password) ? user : default;
		}

		public bool Create(TUser user, string password = null)
		{
			if(user == null)
				throw new ArgumentNullException(nameof(user));

			//更新创建时间
			user.Creation = DateTime.Now;
			user.Modification = null;

			//确认待创建的用户实体
			this.OnCreating(user);

			//验证指定的名称是否合法
			this.OnValidateName(user.Name);

			//确认新密码是否符合密码规则
			this.OnValidatePassword(password);

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				if(this.DataAccess.Insert(user) < 1)
					return false;

				//有效的密码不能为空或全空格字符串
				if(!string.IsNullOrWhiteSpace(password))
					this.SetPassword(user.UserId, password);

				//提交事务
				transaction.Commit();
			}

			//通知用户创建完成
			this.OnCreated(user);

			return true;
		}

		public int Create(IEnumerable<TUser> users)
		{
			if(users == null)
				return 0;

			foreach(var user in users)
			{
				if(user == null)
					continue;

				//更新创建时间
				user.Creation = DateTime.Now;
				user.Modification = null;

				//确认待创建的用户实体
				this.OnCreating(user);

				//验证指定的名称是否合法
				this.OnValidateName(user.Name);
			}

			var count = this.DataAccess.InsertMany(users);

			if(count > 0)
			{
				foreach(var user in users)
				{
					if(user != null && user.UserId > 0)
						this.OnCreated(user);
				}
			}

			return count;
		}

		public TUser Register(string @namespace, string identity, string token, IDictionary<string, object> parameters = null)
		{
			return this.Register(@namespace, identity, token, null, parameters);
		}

		public TUser Register(string @namespace, string identity, string token, string password, IDictionary<string, object> parameters = null)
		{
			if(string.IsNullOrEmpty(token))
				throw new ArgumentNullException(nameof(token));

			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			var index = token.IndexOf(':');

			if(index <= 0 || index == token.Length - 1)
				throw new ArgumentException("Invalid authority argument value.", nameof(token));

			var type = UserIdentityTypeUtility.Detect(identity);
			var user = this.CreateUser(parameters);
			user.Namespace = string.IsNullOrWhiteSpace(@namespace) ? null : @namespace.Trim();

			switch(type)
			{
				case UserIdentityType.Name:
					user.Name = identity;
					user.Nickname = identity;
					break;
				case UserIdentityType.Phone:
					user.Phone = identity;
					user.Nickname = identity.Mask(3, 4);
					break;
				case UserIdentityType.Email:
					user.Email = identity.Mask(2, 2, 0, identity.IndexOf('@'));
					break;
			}

			return this.Create(user, password) ? user : default;
		}

		public int Delete(params uint[] ids)
		{
			if(ids == null || ids.Length < 1)
				return 0;

			if(ids.Contains(GetUserId(0)))
				throw new ArgumentException("You cannot include yourself in the want to delete.");

			EnsureRoles();

			return this.DataAccess.Delete<TUser>(
				Condition.In(nameof(IUserModel.UserId), ids) &
				Condition.NotEqual(nameof(IUserModel.Name), IUserModel.Administrator),
				"Permissions,PermissionFilters");
		}

		public bool Update(uint userId, TUser user)
		{
			if(user == null)
				throw new ArgumentNullException(nameof(user));

			if(user is not IModel model || !model.HasChanges())
				return false;

			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			if(model.HasChanges(nameof(IUserModel.Name)) && !string.IsNullOrWhiteSpace(user.Name))
			{
				//验证指定的名称是否为系统内置名
				if(string.Equals(user.Name, IUserModel.Administrator, StringComparison.OrdinalIgnoreCase))
					throw new SecurityException("username.illegality", "The user name specified to be update cannot be a built-in name.");

				//验证指定的名称是否合法
				this.OnValidateName(user.Name);
			}

			//验证指定的命名空间是否合规
			if(model.HasChanges(nameof(IUserModel.Namespace)))
			{
				var @namespace = ApplicationContext.Current.Principal.Identity.GetNamespace();

				if(string.IsNullOrEmpty(@namespace))
					user.Namespace = string.IsNullOrWhiteSpace(user.Namespace) ? null : user.Namespace.Trim();
				else
					user.Namespace = @namespace;
			}

			if(this.DataAccess.Update(user, new Condition(nameof(IUserModel.UserId), userId), "*,!Name,!Status,!StatusTimestamp") > 0)
			{
				foreach(var entry in model.GetChanges())
				{
					this.OnChanged(userId, entry.Key, entry.Value);
				}

				return true;
			}

			return false;
		}
		#endregion

		#region 密码管理
		public virtual bool HasPassword(uint userId)
		{
			return this.DataAccess.Exists<TUser>(Condition.Equal(nameof(IUserModel.UserId), GetUserId(userId)) & Condition.NotEqual("Password", null));
		}

		public virtual bool HasPassword(string identity, string @namespace = null)
		{
			return this.DataAccess.Exists<TUser>(
							MembershipUtility.GetIdentityCondition(identity) &
							this.GetNamespace(@namespace) &
							Condition.NotEqual("Password", null));
		}

		public bool ChangePassword(uint userId, string oldPassword, string newPassword)
		{
			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			//确认新密码是否符合密码规则
			this.OnValidatePassword(newPassword);

			//获取验证失败的解决器
			var attempter = this.Attempter;
			var attempterKey = $"User.ChangePassword:{userId}";

			//确认验证失败是否超出限制数，如果超出则抛出账号被禁用的异常
			if(attempter != null && !attempter.CheckAsync(attempterKey).AsTask().GetAwaiter().GetResult())
				throw new SecurityException(nameof(SecurityReasons.AccountSuspended));

			//获取用户密码及密码盐
			var token = this.GetPassword(userId);

			if(token.UserId == 0)
				return false;

			if(!PasswordUtility.VerifyPassword(oldPassword, token.Password, token.PasswordSalt))
			{
				//通知验证尝试失败
				if(attempter != null)
					attempter.FailAsync(attempterKey).AsTask().GetAwaiter().GetResult();

				//抛出验证失败异常
				throw new SecurityException(SecurityReasons.InvalidPassword);
			}

			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				attempter.DoneAsync(attempterKey).AsTask().GetAwaiter().GetResult();

			var passwordSalt = GetPasswordSalt();

			return string.IsNullOrEmpty(newPassword) ?
				this.SetPassword(userId, null, 0) :
				this.SetPassword(userId, PasswordUtility.HashPassword(newPassword, passwordSalt), passwordSalt);
		}

		public string ForgetPassword(string identity, string @namespace = null)
		{
			if(string.IsNullOrEmpty(identity))
				throw new ArgumentNullException(nameof(identity));

			var secretor = this.Secretor ?? throw new InvalidOperationException($"Missing the required secretor.");

			//解析用户标识的查询条件
			var condition = MembershipUtility.GetIdentityCondition(identity, out var identityType);

			//如果查询条件解析失败或用户标识为用户名，则抛出不支持的异常
			if(condition == null || identityType == UserIdentityType.Name)
				throw new NotSupportedException("Invalid user identity for the forget password operation.");

			//获取指定标识的用户信息
			var user = this.DataAccess.Select<IUserModel>(condition & this.GetNamespace(@namespace)).FirstOrDefault();

			if(user == null)
				return null;

			var token = Randomizer.GenerateString();

			switch(identityType)
			{
				case UserIdentityType.Email:
					//如果用户的邮箱地址为空，即无法通过邮箱寻回
					if(string.IsNullOrWhiteSpace(user.Email))
						throw new InvalidOperationException("The user's email is unset.");

					//生成校验密文
					var secret = secretor.Generate($"{KEY_FORGET_SECRET}:{token}", user.UserId.ToString());

					//发送忘记密码的邮件通知
					CommandExecutor.Default.Execute($"email.send -template:{KEY_AUTHENTICATION_TEMPLATE} {user.Email}", new
					{
						Code = secret,
						Data = user,
					});

					break;
				case UserIdentityType.Phone:
					//如果用户的电话号码为空，即无法通过短信寻回
					if(string.IsNullOrWhiteSpace(user.Phone))
						throw new InvalidOperationException("The user's phone-number is unset.");

					//生成校验密文
					secret = secretor.Generate($"{KEY_FORGET_SECRET}:{token}", user.UserId.ToString());

					//发送忘记密码的短信通知
					CommandExecutor.Default.Execute($"phone.send -template:{KEY_AUTHENTICATION_TEMPLATE} {user.Phone}", new
					{
						Code = secret,
						Data = user,
					});

					break;
				default:
					throw new SecurityException("Invalid user identity for the forget password operation.");
			}

			//返回执行密码重置令牌
			return token;
		}

		public bool ResetPassword(string token, string secret, string newPassword = null)
		{
			if(string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
				return false;

			var secretor = this.Secretor ?? throw new InvalidOperationException($"Missing the required secretor.");

			//如果重置密码的校验码验证成功
			if(secretor.Verify($"{KEY_FORGET_SECRET}:{token}", secret, out var extra) && uint.TryParse(extra, out var userId))
			{
				//确认新密码是否符合密码规则
				this.OnValidatePassword(newPassword);

				//更新用户的新密码
				return this.SetPassword(userId, newPassword);
			}

			//重置密码校验失败，抛出异常
			throw new SecurityException("verify.fail", "The secret verify fail for the operation.");
		}

		public bool ResetPassword(string identity, string @namespace, string[] passwordAnswers, string newPassword)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			if(passwordAnswers == null || passwordAnswers.Length < 3)
				throw new ArgumentNullException(nameof(passwordAnswers));

			//获取密码问答的答案设置
			var answers = this.GetSecretAnswer(identity, @namespace, out var userId);

			if(userId == 0)
				return false;

			//如果指定的用户没有设置密码问答，则抛出安全异常
			if(answers == null || answers.Count == 0)
				throw new SecurityException("Can not reset password, because the specified user's password questions and answers is unset.");

			//如果密码问答的答案验证失败，则抛出安全异常
			if(passwordAnswers.Length != answers.Count)
				throw new SecurityException("Verification:PasswordAnswers", "The password answers verify failed.");

			//如果密码问答的答案验证失败，则抛出安全异常
			for(int i = 0; i < passwordAnswers.Length; i++)
			{
				if(!PasswordUtility.VerifyPassword(passwordAnswers[i], answers[i], GetPasswordAnswerSalt(userId, i + 1)))
					throw new SecurityException("Verification:PasswordAnswers", "The password answers verify failed.");
			}

			//确认新密码是否符合密码规则
			this.OnValidatePassword(newPassword);

			//重新生成密码随机数
			var passwordSalt = GetPasswordSalt();

			return this.SetPassword(userId, PasswordUtility.HashPassword(newPassword, passwordSalt), passwordSalt);
		}

		public string[] GetPasswordQuestions(uint userId)
		{
			return this.GetSecretQuestions(Condition.Equal(nameof(IUserModel.UserId), GetUserId(userId)));
		}

		public string[] GetPasswordQuestions(string identity, string @namespace = null)
		{
			return this.GetSecretQuestions(MembershipUtility.GetIdentityCondition(identity) & this.GetNamespace(@namespace));
		}

		public bool SetPasswordQuestionsAndAnswers(uint userId, string password, string[] passwordQuestions, string[] passwordAnswers)
		{
			if(passwordQuestions == null || passwordQuestions.Length < 3)
				throw new ArgumentNullException(nameof(passwordQuestions));

			if(passwordAnswers == null || passwordAnswers.Length < 3)
				throw new ArgumentNullException(nameof(passwordAnswers));

			if(passwordQuestions.Length != passwordAnswers.Length)
				throw new ArgumentException("The password questions and answers count is not equals.");

			//确认指定的用户编号是否有效
			userId = GetUserId(userId);

			//获取用户密码及密码盐
			var token = this.GetPassword(userId);

			if(token.UserId == 0)
				return false;

			if(!PasswordUtility.VerifyPassword(password, token.Password, token.PasswordSalt))
				throw new SecurityException(SecurityReasons.InvalidPassword, "The password verify failed.");

			return this.SetSecretAnswers(userId, passwordQuestions, passwordAnswers);
		}
		#endregion

		#region 秘密校验
		public bool Verify(uint userId, string type, string secret)
		{
			if(string.IsNullOrWhiteSpace(type))
				throw new ArgumentNullException(nameof(type));

			//校验指定的密文
			var succeed = this.Secretor.Verify($"{type}:{userId}", secret, out var extra);

			//如果校验成功并且密文中有附加数据
			if(succeed && (extra != null && extra.Length > 0))
			{
				switch(type)
				{
					case KEY_EMAIL_SECRET:
						if(this.DataAccess.Update<TUser>(new
						{
							Email = string.IsNullOrWhiteSpace(extra) ? null : extra.Trim(),
							Modification = DateTime.Now,
						}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0)
							this.OnChanged(userId, nameof(IUserModel.Email), extra);

						break;
					case KEY_PHONE_SECRET:
						if(this.DataAccess.Update<TUser>(new
						{
							Phone = string.IsNullOrWhiteSpace(extra) ? null : extra.Trim(),
							Modification = DateTime.Now,
						}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0)
							this.OnChanged(userId, nameof(IUserModel.Phone), extra);

						break;
				}
			}

			return succeed;
		}
		#endregion

		#region 抽象方法
		protected abstract TUser CreateUser(IDictionary<string, object> parameters);
		#endregion

		#region 虚拟方法
		protected virtual void OnCreating(TUser user)
		{
			if(string.IsNullOrWhiteSpace(user.Name))
			{
				if(string.IsNullOrWhiteSpace(user.Phone) && string.IsNullOrWhiteSpace(user.Email))
					throw new ArgumentException("The user name is empty.");

				//虽然用户名为空但是指定了绑定的“Phone”或“Email”，则将用户名设置为随机值
				user.Name = "$U" + Randomizer.GenerateString();
			}

			if(string.IsNullOrWhiteSpace(user.Namespace))
				user.Namespace = null;
		}

		protected virtual void OnCreated(TUser user) { }

		protected virtual PasswordToken GetPassword(uint userId)
		{
			return this.DataAccess.Select<PasswordToken>(
				this.DataAccess.Naming.Get<TUser>(),
				Condition.Equal(nameof(IUserModel.UserId), userId)).FirstOrDefault();
		}

		protected bool SetPassword(uint userId, string password)
		{
			if(string.IsNullOrWhiteSpace(password))
				return this.SetPassword(userId, null, 0);

			//重新生成密码随机数
			var passwordSalt = GetPasswordSalt();

			return this.SetPassword(userId, PasswordUtility.HashPassword(password, passwordSalt), passwordSalt);
		}

		protected virtual bool SetPassword(uint userId, byte[] password, long passwordSalt)
		{
			if(password == null || password.Length == 0)
				return this.DataAccess.Update<TUser>(new
				{
					Password = DBNull.Value,
					PasswordSalt = DBNull.Value,
				}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0;

			return this.DataAccess.Update<TUser>(new
			{
				Password = password,
				PasswordSalt = passwordSalt,
			}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0;
		}

		protected virtual string[] GetSecretQuestions(ICondition criteria)
		{
			if(criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var question = this.DataAccess.Select<string>(this.DataAccess.Naming.Get<TUser>(), criteria, nameof(UserSecretRecord.SecretQuestion)).FirstOrDefault();

			if(string.IsNullOrEmpty(question))
				return null;

			return question.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}

		protected virtual IReadOnlyList<byte[]> GetSecretAnswer(string identity, string @namespace, out uint userId)
		{
			var record = this.DataAccess.Select<UserSecretRecord>(
				this.DataAccess.Naming.Get<TUser>(),
				MembershipUtility.GetIdentityCondition(identity) & this.GetNamespace(@namespace),
				$"{nameof(UserSecretRecord.UserId)}," +
				$"{nameof(UserSecretRecord.SecretQuestion)}," +
				$"{nameof(UserSecretRecord.SecretAnswer)}").FirstOrDefault();

			userId = record.UserId;

			if(record.UserId == 0)
				return null;

			if(record.SecretAnswer != null && record.SecretAnswer.Length > 0)
			{
				var count = record.SecretAnswer[0];

				if(count > 0 && count <= 5)
				{
					var answers = new List<byte[]>(count);
					var buffer = new byte[(record.SecretAnswer.Length - 1) / count];

					for(int i = 0; i < count; i++)
					{
						Array.Copy(record.SecretAnswer, i * buffer.Length + 1, buffer, 0, buffer.Length);
						answers.Add(buffer);
					}

					return answers;
				}
			}

			return Array.Empty<byte[]>();
		}

		protected virtual bool SetSecretAnswers(uint userId, string[] questions, string[] answers)
		{
			if((questions == null && answers == null) ||
			   (questions.Length == 0 && answers.Length == 0))
			{
				return this.DataAccess.Update<TUser>(new
				{
					SecretQuestion = DBNull.Value,
					SecretAnswer = DBNull.Value
				}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0;
			}

			if(questions.Length != answers.Length)
				throw new ArgumentException($"The specified '{nameof(questions)}' parameter does not match the length of the '{nameof(answers)}' parameter.");

			if(questions.Length > 5)
				throw new ArgumentOutOfRangeException(nameof(questions));

			var buffer = new List<byte>(61)
			{
				(byte)answers.Length
			};

			for(int i = 0; i < answers.Length; i++)
				buffer.AddRange(HashPasswordAnswer(answers[i], userId, i + 1));

			return this.DataAccess.Update<TUser>(new
			{
				SecretQuestion = string.Join('\n', questions),
				SecretAnswer = buffer.ToArray()
			}, Condition.Equal(nameof(IUserModel.UserId), userId)) > 0;
		}

		protected virtual void OnChangeEmail(IUserModel user, string email)
		{
			if(user == null)
				return;

			var secret = this.Secretor.Generate($"{KEY_EMAIL_SECRET}:{user.UserId}", email);

			CommandExecutor.Default.Execute($"email.send -template:{KEY_EMAIL_SECRET} {email}", new
			{
				Code = secret,
				Data = user,
			});
		}

		protected virtual void OnChangePhone(IUserModel user, string phone)
		{
			if(user == null)
				return;

			var secret = this.Secretor.Generate($"{KEY_PHONE_SECRET}:{user.UserId}", phone);

			CommandExecutor.Default.Execute($"phone.send -template:{KEY_IMPORTANT_CHANGE_TEMPLATE} {phone}", new
			{
				Code = secret,
				Data = user,
			});
		}

		protected virtual void OnValidateName(string name)
		{
			var validator = this.ServiceProvider?.Resolve<IValidator<string>>("user.name");

			if(validator != null)
				validator.Validate(name, message => throw new SecurityException("username.illegality", message));
		}

		protected virtual void OnValidatePassword(string password)
		{
			var validator = this.ServiceProvider?.Resolve<IValidator<string>>("password");

			if(validator != null)
				validator.Validate(password, message => throw new SecurityException("password.illegality", message));
		}

		protected virtual bool IsVerifyEmailRequired() => true;
		protected virtual bool IsVerifyPhoneRequired() => true;
		#endregion

		#region 激发事件
		protected virtual void OnChanged(uint userId, string propertyName, object propertyValue)
		{
			this.Changed?.Invoke(this, new ChangedEventArgs(userId, propertyName, propertyValue));
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Condition GetNamespace(string @namespace) => Mapping.Instance.Namespace.GetCondition(this.DataAccess.Naming.Get<TUser>(), @namespace);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static uint GetUserId(uint userId)
		{
			if(userId == 0)
				return ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();

			/*
			 * 只有当前用户是如下情况之一，才能操作指定的其他用户：
			 *   1) 指定的用户就是当前用户自己；
			 *   2) 当前用户是系统管理员(Administrators)或安全管理员角色(Security)成员。
			 */

			var current = ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();

			if(current == userId || ApplicationContext.Current.Principal.InRoles(new[] { IRoleModel.Administrators, IRoleModel.Security }))
				return userId;

			throw new AuthorizationException($"The current user cannot operate on other user information.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static uint GetUserId(uint userId, out bool secured)
		{
			/*
			 * 只有当前用户是如下情况之一，才能操作指定的其他用户：
			 *   1) 指定的用户就是当前用户自己；
			 *   2) 当前用户是系统管理员(Administrators)或安全管理员角色(Security)成员。
			 */

			if(userId == 0)
				userId = ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();

			var current = ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();
			secured = ApplicationContext.Current.Principal.InRoles(new[] { IRoleModel.Administrators, IRoleModel.Security });

			if(current == userId || secured)
				return userId;

			throw new AuthorizationException($"The current user cannot operate on other user information.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static long GetPasswordSalt() => Math.Abs(Randomizer.GenerateInt64());

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static byte[] GetPasswordAnswerSalt(uint userId, int index) => Encoding.ASCII.GetBytes($"Zongsoft.Security.User:{userId}:Password.Answer[{index}]");

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static byte[] HashPasswordAnswer(string answer, uint userId, int index)
		{
			if(string.IsNullOrEmpty(answer))
				return null;

			var salt = GetPasswordAnswerSalt(userId, index);
			return PasswordUtility.HashPassword(answer, salt);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static void EnsureRoles()
		{
			if(!ApplicationContext.Current.Principal.InRoles(new[] { IRoleModel.Administrators, IRoleModel.Security }))
				throw new AuthorizationException("Denied: The current user is not a security administrator and is not authorized to perform this operation.");
		}
		#endregion

		#region 内部结构
		protected struct PasswordToken
		{
			public uint UserId { get; set; }
			public byte[] Password { get; set; }
			public long PasswordSalt { get; set; }
		}

		private struct UserSecretRecord
		{
			public uint UserId { get; set; }
			public string SecretQuestion { get; set; }
			public byte[] SecretAnswer { get; set; }
		}
		#endregion
	}
}
