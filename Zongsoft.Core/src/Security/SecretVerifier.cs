/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security
{
	[Service(typeof(IIdentityVerifier))]
	public class SecretVerifier : IIdentityVerifier, IMatchable<string>
	{
		#region 常量定义
		private const string KEY_SECRET = "verifier.secret";
		#endregion

		#region 构造函数
		public SecretVerifier() { }
		#endregion

		#region 公共属性
		public string Name => "Secret";

		[ServiceDependency]
		public ISecretor Secretor { get; set; }
		#endregion

		#region 公共方法
		public IdentityVerifierResult Issue(string key, string extra, IDictionary<string, object> parameters = null)
		{
			var secret = this.Secretor.Generate($"{KEY_SECRET}:{key}");

			if(parameters == null && extra != null && extra.Length > 0)
				parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			foreach(var entry in this.ResolveExtra(extra))
				parameters[entry.Key] = entry.Value;

			if(key.Contains('@'))
				return this.IssueEmail(key, this.GetTemplate(parameters), secret, parameters);
			else
				return this.IssuePhone(key, this.GetTemplate(parameters), secret, parameters);
		}

		public bool Verify(string key, string token, IDictionary<string, object> parameters = null)
		{
			if(string.IsNullOrWhiteSpace(key))
				throw new ArgumentNullException(nameof(key));

			return this.Secretor.Verify($"{KEY_SECRET}:{key}", token, out _);
		}
		#endregion

		#region 虚拟方法
		protected virtual string GetTemplate(IDictionary<string, object> parameters)
		{
			if(parameters != null && parameters.TryGetValue("template", out var value) && value is string text)
				return text;

			return KEY_SECRET;
		}

		protected virtual IdentityVerifierResult IssueEmail(string key, string template, string secret, IDictionary<string, object> parameters)
		{
			try
			{
				CommandExecutor.Default.Execute($"email.send -template:{template} {key}", new
				{
					Code = secret,
					Data = parameters,
				});

				return IdentityVerifierResult.Success(key, secret, parameters);
			}
			catch(Exception ex)
			{
				return IdentityVerifierResult.Fail(key, ex, parameters);
			}
		}

		protected virtual IdentityVerifierResult IssuePhone(string key, string template, string secret, IDictionary<string, object> parameters)
		{
			try
			{
				if(parameters != null &&
				   parameters.TryGetValue("channel", out var value) &&
				   value is string text &&
				   string.Equals(text, "voice", StringComparison.OrdinalIgnoreCase))
					CommandExecutor.Default.Execute($"phone.call -template:{template} {key}", new
					{
						Code = secret,
						Data = parameters,
					});
				else
					CommandExecutor.Default.Execute($"phone.send -template:{template} {key}", new
					{
						Code = secret,
						Data = parameters,
					});

				return IdentityVerifierResult.Success(key, secret, parameters);
			}
			catch(Exception ex)
			{
				return IdentityVerifierResult.Fail(key, ex, parameters);
			}
		}
		#endregion

		#region 私有方法
		private IEnumerable<KeyValuePair<string, string>> ResolveExtra(string extra)
		{
			if(string.IsNullOrEmpty(extra))
				yield break;

			var parts = extra.Slice(';');

			foreach(var part in parts)
			{
				var index = part.IndexOfAny(new[] { '=', ':' });

				if(index < 0)
					yield return new KeyValuePair<string, string>(part, null);
				else if(index == 0)
					yield return new KeyValuePair<string, string>(string.Empty, part.Substring(1));
				else if(index == part.Length - 1)
					yield return new KeyValuePair<string, string>(part.Substring(0, index), null);
				else
					yield return new KeyValuePair<string, string>(part.Substring(0, index), part.Substring(index + 1));
			}
		}
		#endregion

		#region 匹配方法
		bool IMatchable<string>.Match(string parameter) => this.Name.Equals(parameter, StringComparison.OrdinalIgnoreCase);
		bool IMatchable.Match(object parameter) => this.Name.Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
}
