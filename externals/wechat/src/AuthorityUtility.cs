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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat
{
	public static class AuthorityUtility
	{
		#region 静态字段
		private static readonly IDictionary<string, IAuthority> _authorities = new Dictionary<string, IAuthority>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 获取机构
		public static IAuthority GetAuthority(string name = null)
		{
			if(name == null)
				name = string.Empty;

			if(_authorities.TryGetValue(name, out var authority) && authority != null)
				return authority;

			lock(_authorities)
			{
				if(_authorities.TryGetValue(name, out authority))
					return authority;

				authority = CreateAuthority(name);

				if(authority == null)
					return null;

				if(string.IsNullOrEmpty(name))
					_authorities.TryAdd(string.Empty, authority);

				return _authorities.TryAdd(authority.Name, authority) ? authority : _authorities[authority.Name];
			}
		}

		private static IAuthority CreateAuthority(string name)
		{
			Options.AuthorityOptions options;

			if(string.IsNullOrEmpty(name))
			{
				options = Utility.GetOptions<Options.AuthorityOptionsCollection>("/Externals/Wechat/Authorities")?.GetDefault() ??
					throw new WechatException("The configuration section for the default authority of the WeChat was not found.");
			}
			else
			{
				options = Utility.GetOptions<Options.AuthorityOptions>($"/Externals/Wechat/Authorities/{name}") ??
					throw new WechatException($"The configuration section for the '{name}' authority of the WeChat was not found.");
			}

			if(string.IsNullOrEmpty(options.Code))
				throw new WechatException($"Invalid configuration section for the '{name}' authority of the WeChat.");

			var certificate = GetCertificate(options.Directory, options.Code);
			if(certificate == null)
				throw new WechatException($"No certificate found for '{options.Code}({options.Name})' authority of the WeChat.");

			return new Authority(options.Name, options.Code, options.Secret, certificate, new AccountCollection(options.Apps));
		}
		#endregion

		#region 获取证书
		private static ICertificate GetCertificate(DirectoryInfo directory, string authorityCode)
		{
			if(string.IsNullOrEmpty(authorityCode))
				throw new ArgumentNullException(nameof(authorityCode));

			if(directory == null || !directory.Exists)
				return null;

			var directories = directory.GetDirectories(authorityCode);

			if(directories != null && directories.Length > 0)
				directory = directories[0];

			var files = directory.GetFiles(authorityCode + "*");

			if(files == null || files.Length == 0)
				files = directory.GetFiles();

			FileInfo file = null;

			for(int i = 0; i < files.Length; i++)
			{
				if(file == null)
					file = files[i];
				else if(files[i].CreationTimeUtc > file.CreationTimeUtc)
					file = files[i];
			}

			return ResolveFile(file, authorityCode);
		}

		private static ICertificate ResolveFile(FileInfo file, string authorityCode)
		{
			if(file == null || !file.Exists)
				return null;

			return Certificate.FromPem(
				Path.GetFileNameWithoutExtension(file.Name),
				file.OpenRead(),
				default,
				new Certificate.CertificateIssuer(authorityCode),
				new Certificate.CertificateSubject(authorityCode)
			);
		}
		#endregion
	}
}
