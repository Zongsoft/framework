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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

partial class UserServiceBase<TUser>
{
	#region 成员字段
	private IAttempter _attempter;
	#endregion

	#region 公共属性
	public IAttempter Attempter
	{
		get => _attempter ?? Authentication.Attempter;
		set => _attempter = value;
	}
	#endregion

	#region 密码操作
	public ValueTask<bool> HasPasswordAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		return this.Accessor.ExistsAsync(
			this.Name,
			this.GetCriteria(identifier) & Condition.NotEqual("Password", null),
			DataExistsOptions.SuppressValidator(),
			cancellation: cancellation);
	}

	public async ValueTask<bool> ChangePasswordAsync(Identifier identifier, string oldPassword, string newPassword, CancellationToken cancellation = default)
	{
		const string ATTEMPTER_PREFIX = "User.Password.Change";

		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		//确认新密码是否符合密码规则
		this.OnValidatePassword(newPassword);

		//获取验证失败的解决器
		var attempter = this.Attempter;

		//确认验证失败是否超出限制数，如果超出则抛出账号被禁用的异常
		if(attempter != null && !await attempter.CheckAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation))
			throw new SecurityException(nameof(SecurityReasons.AccountSuspended));

		//获取用户密钥信息
		var cipher = await this.Passworder.GetAsync(identifier, cancellation);
		if(cipher == null)
			return false;

		if(await this.Passworder.VerifyAsync(oldPassword, cipher, cancellation))
		{
			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				await attempter.DoneAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation);
		}
		else
		{
			//通知验证尝试失败
			if(attempter != null)
				await attempter.FailAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation);

			//抛出验证失败异常
			throw new SecurityException(SecurityReasons.InvalidPassword);
		}

		//将用户密钥重置为新密码
		cipher.Reset(newPassword);

		//设置指定用户的新密钥
		return await this.Passworder.SetAsync(identifier, cipher, cancellation);
	}

	public ValueTask<string> ForgetPasswordAsync(string identity, string @namespace, Parameters parameters, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identity))
			throw new ArgumentNullException(nameof(identity));

		var secretor = this.Secretor ?? throw new InvalidOperationException($"Missing the required secretor.");

		//获取指定标识的用户
		var user = this.Accessor.SelectAsync<TUser>(this.Name, this.GetCriteria(identity, @namespace, out var identityType), cancellation).Synchronize(cancellation).FirstOrDefault();
		if(user == null)
			return ValueTask.FromResult<string>(null);

		//设置发送方案（手机、邮箱）
		if(parameters == null)
			parameters = new Parameters([new KeyValuePair<string, object>("scheme", identityType)]);
		else
			parameters.TryAdd("scheme", identityType);

		return this.OnForgetPasswordAsync(user, parameters, cancellation);
	}

	public ValueTask<bool> ResetPasswordAsync(string token, string secret, string password = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(token))
			throw new ArgumentNullException(nameof(token));

		if(string.IsNullOrEmpty(secret))
			return ValueTask.FromResult(false);

		var secretor = this.Secretor ?? throw new InvalidOperationException($"Missing the required secretor.");

		//如果重置密码的校验码验证成功
		if(secretor.Verify(token, secret, out var extra) && !string.IsNullOrEmpty(extra))
		{
			//确认新密码是否符合密码规则
			this.OnValidatePassword(password);

			//更新用户的新密码
			return this.Passworder.SetAsync(new Identifier(typeof(TUser), extra), password, cancellation);
		}

		//返回重置密码失败
		return ValueTask.FromResult(false);
	}

	public async ValueTask<bool> ResetPasswordAsync(string identity, string @namespace, string[] passwordAnswers, string newPassword = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identity))
			throw new ArgumentNullException(nameof(identity));

		if(passwordAnswers == null || passwordAnswers.Length < 3)
			throw new ArgumentNullException(nameof(passwordAnswers));

		//获取指定标识的用户
		var user = this.Accessor.SelectAsync<TUser>(this.Name, this.GetCriteria(identity, @namespace, out var identityType), cancellation).Synchronize(cancellation).FirstOrDefault();
		if(user == null)
			return false;

		//获取密码问答的答案设置
		var answers = await this.GetSecretAnswerAsync(user.Identifier, cancellation);

		//如果指定的用户没有设置密码问答，则抛出安全异常
		if(answers == null || answers.Count == 0)
			return false;

		//如果密码问答的答案验证失败，则抛出安全异常
		if(passwordAnswers.Length != answers.Count)
			throw new SecurityException("Verification:PasswordAnswers", "The password answers verify failed.");

		//如果密码问答的答案验证失败，则抛出安全异常
		for(int i = 0; i < passwordAnswers.Length; i++)
		{
			if(!PasswordUtility.VerifyPassword(passwordAnswers[i], answers[i], GetPasswordAnswerSalt(user.Identifier, i + 1)))
				throw new SecurityException("Verification:PasswordAnswers", "The password answers verify failed.");
		}

		//确认新密码是否符合密码规则
		this.OnValidatePassword(newPassword);

		//更新用户的新密码
		return await this.Passworder.SetAsync(
			user.Identifier,
			newPassword,
			cancellation);
	}

	public virtual async ValueTask<string[]> GetPasswordQuestionsAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		var result = this.Accessor.SelectAsync<SecretRecord?>(
			this.Name,
			this.GetCriteria(identifier),
			cancellation);

		await using var enumerator = result.GetAsyncEnumerator(cancellation);
		if(await enumerator.MoveNextAsync())
		{
			if(enumerator.Current.HasValue && !string.IsNullOrEmpty(enumerator.Current.Value.SecretQuestion))
				return enumerator.Current.Value.SecretQuestion.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}

		return null;
	}

	public virtual async ValueTask<string[]> GetPasswordQuestionsAsync(string identity, string @namespace, CancellationToken cancellation = default)
	{
		var result = this.Accessor.SelectAsync<SecretRecord?>(
			this.Name,
			this.GetCriteria(identity, @namespace),
			cancellation);

		await using var enumerator = result.GetAsyncEnumerator(cancellation);
		if(await enumerator.MoveNextAsync())
		{
			if(enumerator.Current.HasValue && !string.IsNullOrEmpty(enumerator.Current.Value.SecretQuestion))
				return enumerator.Current.Value.SecretQuestion.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}

		return null;
	}

	public async ValueTask<bool> SetPasswordQuestionsAndAnswersAsync(Identifier identifier, string password, string[] passwordQuestions, string[] passwordAnswers, CancellationToken cancellation = default)
	{
		const string ATTEMPTER_PREFIX = "User.Password.Answers";

		if(passwordQuestions == null || passwordQuestions.Length < 3)
			throw new ArgumentNullException(nameof(passwordQuestions));

		if(passwordAnswers == null || passwordAnswers.Length < 3)
			throw new ArgumentNullException(nameof(passwordAnswers));

		if(passwordQuestions.Length != passwordAnswers.Length)
			throw new ArgumentException("The password questions and answers count is not equals.");

		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		//获取验证失败的解决器
		var attempter = this.Attempter;

		//确认验证失败是否超出限制数，如果超出则抛出账号被禁用的异常
		if(attempter != null && !await attempter.CheckAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation))
			throw new SecurityException(nameof(SecurityReasons.AccountSuspended));

		//获取用户密钥信息
		var cipher = await this.Passworder.GetAsync(identifier, cancellation);
		if(cipher == null)
			return false;

		if(await this.Passworder.VerifyAsync(password, cipher, cancellation))
		{
			//通知验证尝试成功，即清空验证失败记录
			if(attempter != null)
				await attempter.DoneAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation);
		}
		else
		{
			//通知验证尝试失败
			if(attempter != null)
				await attempter.FailAsync($"{ATTEMPTER_PREFIX}#{identifier.Value}", cancellation);

			//抛出验证失败异常
			throw new SecurityException(SecurityReasons.InvalidPassword);
		}

		return await this.SetSecretAnswersAsync(identifier, passwordQuestions, passwordAnswers, cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnValidatePassword(string password)
	{
		var validator = this.Services?.Resolve<IValidator<string>>("password");
		validator?.Validate(password, message => throw new SecurityException("password.illegality", message));
	}

	protected virtual ValueTask<string> OnForgetPasswordAsync(TUser user, Parameters parameters, CancellationToken cancellation)
	{
		if(parameters.TryGetValue("scheme", out var value) && value is string scheme)
		{
			var destination = GetDestination(user, scheme);
			if(string.IsNullOrEmpty(destination))
				return ValueTask.FromResult<string>(null);

			return this.Secretor.Transmitter.TransmitAsync(
				scheme,
				destination,
				GetTemplate(user, parameters),
				GetScenario(user, parameters),
				GetCaptcha(user, parameters),
				GetChannel(user, parameters),
				user.Identifier.Value.ToString(),
				cancellation);
		}

		return ValueTask.FromResult<string>(null);

		static string GetDestination(IUser user, string scheme) => scheme.ToUpperInvariant() switch
		{
			"email" => user.Email,
			"phone" => user.Phone,
			_ => null,
		};

		static string GetTemplate(IUser user, Parameters parameters) => parameters.TryGetValue("template", out var value) && value is string text ? text : "User.Password.Foreget";
		static string GetScenario(IUser user, Parameters parameters) => parameters.TryGetValue("scenario", out var value) && value is string text ? text : null;
		static string GetCaptcha(IUser user, Parameters parameters) => parameters.TryGetValue("captcha", out var value) && value is string text ? text : null;
		static string GetChannel(IUser user, Parameters parameters) => parameters.TryGetValue("channel", out var value) && value is string text ? text : null;
	}

	protected virtual ValueTask<IReadOnlyList<byte[]>> GetSecretAnswerAsync(Identifier identifier, CancellationToken cancellation)
	{
		if(identifier.IsEmpty)
			return ValueTask.FromResult<IReadOnlyList<byte[]>>(null);

		var record = this.Accessor.SelectAsync<SecretRecord?>(
			this.Name,
			this.GetCriteria(identifier),
			cancellation).Synchronize(cancellation).FirstOrDefault();

		if(record == null)
			return ValueTask.FromResult<IReadOnlyList<byte[]>>(null);

		if(record.Value.SecretAnswer != null && record.Value.SecretAnswer.Length > 0)
		{
			var count = record.Value.SecretAnswer[0];

			if(count > 0 && count <= 5)
			{
				var answers = new List<byte[]>(count);
				var buffer = new byte[(record.Value.SecretAnswer.Length - 1) / count];

				for(int i = 0; i < count; i++)
				{
					Array.Copy(record.Value.SecretAnswer, i * buffer.Length + 1, buffer, 0, buffer.Length);
					answers.Add(buffer);
				}

				return ValueTask.FromResult<IReadOnlyList<byte[]>>(answers);
			}
		}

		return ValueTask.FromResult<IReadOnlyList<byte[]>>(null);
	}

	protected virtual async ValueTask<bool> SetSecretAnswersAsync(Identifier identifier, string[] questions, string[] answers, CancellationToken cancellation)
	{
		if((questions == null && answers == null) || (questions.Length == 0 && answers.Length == 0))
		{
			return await this.Accessor.UpdateAsync(this.Name, new
			{
				SecretQuestion = DBNull.Value,
				SecretAnswer = DBNull.Value
			}, this.GetCriteria(identifier), cancellation) > 0;
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
			buffer.AddRange(HashPasswordAnswer(answers[i], identifier, i + 1));

		return await this.Accessor.UpdateAsync(this.Name, new
		{
			SecretQuestion = string.Join('\n', questions),
			SecretAnswer = buffer.ToArray()
		}, this.GetCriteria(identifier), cancellation) > 0;
	}
	#endregion

	#region 私有方法
	private static byte[] GetPasswordAnswerSalt(Identifier identifier, int index) => System.Text.Encoding.ASCII.GetBytes($"Zongsoft.Security.User:{identifier.Value}:Password.Answer[{index}]");
	private static byte[] HashPasswordAnswer(string answer, Identifier identifier, int index)
	{
		if(string.IsNullOrEmpty(answer))
			return null;

		var salt = GetPasswordAnswerSalt(identifier, index);
		return PasswordUtility.HashPassword(answer, salt);
	}
	#endregion

	#region 嵌套结构
	private struct SecretRecord
	{
		public string SecretQuestion { get; set; }
		public byte[] SecretAnswer { get; set; }
	}
	#endregion

	#region 嵌套子类
	public abstract class PassworderBase<TCipher>(UserServiceBase<TUser> service) : Passworder where TCipher : Passworder.Cipher, new()
	{
		#region 保护属性
		protected UserServiceBase<TUser> Service { get; } = service ?? throw new ArgumentNullException(nameof(service));
		#endregion

		#region 公共方法
		public override async ValueTask<Cipher> GetAsync(Identifier identifier, CancellationToken cancellation)
		{
			if(identifier.IsEmpty)
				return null;

			var result = this.OnGetAsync(identifier, cancellation);
			await using var enumerator = result.GetAsyncEnumerator(cancellation);
			return await enumerator.MoveNextAsync() ? enumerator.Current : null;
		}

		public override async ValueTask<Cipher> GetAsync(string identity, string @namespace, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(identity))
				return null;

			var result = this.OnGetAsync(identity, @namespace, cancellation);
			await using var enumerator = result.GetAsyncEnumerator(cancellation);
			return await enumerator.MoveNextAsync() ? enumerator.Current : null;
		}

		public override ValueTask<bool> SetAsync(Identifier identifier, Cipher cipher, CancellationToken cancellation)
		{
			if(identifier.IsEmpty)
				return ValueTask.FromResult(false);

			return this.OnSetAsync(identifier, cipher as TCipher, cancellation);
		}

		public override ValueTask<bool> VerifyAsync(string password, Cipher cipher, CancellationToken cancellation)
		{
			return this.OnVerifyAsync(password, cipher as TCipher, cancellation);
		}
		#endregion

		#region 保护方法
		protected override Cipher GetCipher(string password, string algorithm = null)
		{
			var cipher = new TCipher();
			cipher.Reset(password, algorithm);
			return cipher;
		}
		#endregion

		#region 虚拟方法
		protected virtual IAsyncEnumerable<TCipher> OnGetAsync(Identifier identifier, CancellationToken cancellation) =>
			this.Service.Accessor.SelectAsync<TCipher>(this.Service.Name, this.Service.GetCriteria(identifier), cancellation);
		protected virtual IAsyncEnumerable<TCipher> OnGetAsync(string identity, string @namespace, CancellationToken cancellation) =>
			this.Service.Accessor.SelectAsync<TCipher>(this.Service.Name, this.Service.GetCriteria(identity, @namespace), cancellation);
		protected virtual async ValueTask<bool> OnSetAsync(Identifier identifier, TCipher cipher, CancellationToken cancellation) =>
			await this.Service.Accessor.UpdateAsync<TCipher>(this.Service.Name, this.Service.GetCriteria(identifier), cancellation) > 0;
		protected virtual ValueTask<bool> OnVerifyAsync(string password, TCipher cipher, CancellationToken cancellation) =>
			ValueTask.FromResult(PasswordUtility.VerifyPassword(password, cipher.Value, cipher.Nonce, cipher.Name));
		#endregion
	}
	#endregion
}
