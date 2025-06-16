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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Security;

namespace Zongsoft.Externals.Opc.Security;

partial class Authenticator
{
	private sealed class DefaultAuthenticator : Authenticator
	{
		#region 常量定义
		private const string CERTIFICATES_DIRECTORY = "certificates";
		private readonly string FILE_NAME = $"{typeof(OpcServer).Namespace}.Users.ini";
		#endregion

		#region 重写方法
		protected override ValueTask<bool> OnAuthenticateAsync(OpcServer server, AuthenticationIdentity.Account identity, CancellationToken cancellation = default)
		{
			var path = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), FILE_NAME);

			if(!File.Exists(path))
			{
				path = Path.Combine(AppContext.BaseDirectory, FILE_NAME);
				if(!File.Exists(path))
					return ValueTask.FromResult(false);
			}

			var profile = Zongsoft.Configuration.Profiles.Profile.Load(path);

			if(profile.Sections.TryGetValue(server.Name, out var section))
			{
				//从当前服务器用户配置集中查找指定用户名的配置项
				if(section.Entries.TryGetValue(identity.UserName, out var entry))
					return Password.TryParse(entry.Value, out var password) ?
						ValueTask.FromResult(password.Verify(identity.Password)) :
						ValueTask.FromResult(string.Equals(entry.Value, identity.Password));
			}

			//从通用(全局)用户配置集中查找指定用户名的配置项
			if(profile.Entries.TryGetValue(identity.UserName, out var global))
				return Password.TryParse(global.Value, out var password) ?
					ValueTask.FromResult(password.Verify(identity.Password)) :
					ValueTask.FromResult(string.Equals(global.Value, identity.Password));

			return ValueTask.FromResult(false);
		}

		protected override ValueTask<bool> OnAuthenticateAsync(OpcServer server, AuthenticationIdentity.Certificate identity, CancellationToken cancellation = default)
		{
			var path = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), CERTIFICATES_DIRECTORY);

			if(!Directory.Exists(path))
			{
				path = Path.Combine(AppContext.BaseDirectory, CERTIFICATES_DIRECTORY);

				if(!Directory.Exists(path))
					return ValueTask.FromResult(false);
			}

			var files = Directory.EnumerateFiles(path, "*.pem")
				.Concat(Directory.EnumerateFiles(path, "*.der"))
				.Concat(Directory.EnumerateFiles(path, "*.cer"))
				.Concat(Directory.EnumerateFiles(path, "*.pfx"));

			foreach(var file in files)
			{
				var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(file);

				if(certificate != null && certificate.Verify() && certificate.Equals(identity.X509))
					return ValueTask.FromResult(true);
			}

			return ValueTask.FromResult(false);
		}
		#endregion
	}
}
