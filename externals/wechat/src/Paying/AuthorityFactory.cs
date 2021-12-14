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

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class AuthorityFactory
	{
		#region 静态字段
		private static readonly IDictionary<string, IAuthority> _authorities = new Dictionary<string, IAuthority>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 获取机构
		public static IAuthority GetAuthority(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(_authorities.TryGetValue(name, out var authority) && authority != null)
				return authority;

			lock(_authorities)
			{
				if(_authorities.TryGetValue(name, out authority))
					return authority;

				authority = CreateAuthority(name);

				if(authority == null)
					return null;

				return _authorities.TryAdd(name, authority) ? authority : _authorities[name];
			}
		}

		private static IAuthority CreateAuthority(string name)
		{
			var options = Utility.GetOptions<Options.AuthorityOptions>($"/Externals/Wechat/Paying/{name}");
			if(options == null)
				return null;

			var certificate = GetCertificate(options.Directory, options.Code);

			if(certificate == null)
				return null;

			if(certificate.Issuer == null || string.IsNullOrEmpty(certificate.Issuer.Identifier))
				certificate.Issuer = new CertificateIssuer(options.Code, options.Name);

			var app = options.Apps.GetDefault();
			return app == null ? null : new Authority(options.Name, options.Code, options.Secret, new Applet(app.Name, app.Secret), certificate);
		}
		#endregion

		#region 获取证书
		internal static Certificate GetCertificate(DirectoryInfo directory, string subject)
		{
			if(string.IsNullOrEmpty(subject))
				throw new ArgumentNullException(nameof(subject));

			if(directory == null || !directory.Exists)
				return null;

			var directories = directory.GetDirectories(subject);

			if(directories != null && directories.Length > 0)
				directory = directories[0];

			var files = directory.GetFiles(subject + "*");

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

			return ResolveFile(file);
		}

		private static Certificate ResolveFile(FileInfo file)
		{
			if(file == null || !file.Exists)
				return null;

			if(string.IsNullOrEmpty(file.Extension))
				return null;

			switch(file.Extension.ToLowerInvariant())
			{
				case ".bin":
				case ".key":
					using(var stream = file.OpenRead())
					{
						using var memory = new MemoryStream((int)stream.Length);
						stream.CopyTo(memory);
						return CreateCertificate(file, memory.ToArray());
					}
				case ".txt":
				case ".base64":
					using(var stream = file.OpenRead())
					{
						using var reader = new StreamReader(stream);
						var data = Convert.FromBase64String(reader.ReadToEnd());
						return CreateCertificate(file, data);
					}
				case ".pem":
					using(var stream = file.OpenRead())
					{
						using var reader = new StreamReader(stream);
						var text = new System.Text.StringBuilder((int)stream.Length);
						var line = string.Empty;

						do
						{
							line = reader.ReadLine();

							if(string.IsNullOrWhiteSpace(line))
								continue;

							if(line.StartsWith("-----BEGIN "))
								break;
						} while(line != null && !line.StartsWith("-----END "));

						while(!reader.EndOfStream)
						{
							line = reader.ReadLine();

							if(line == null || line.StartsWith("-----END "))
								break;

							text.Append(line);
						}

						if(text.Length == 0)
							return null;

						var data = Convert.FromBase64String(text.ToString());
						return CreateCertificate(file, data);
					}
			}

			return null;

			static Certificate CreateCertificate(FileInfo file, byte[] privateKey)
			{
				var code = Path.GetFileNameWithoutExtension(file.Name);
				var index = code.IndexOf('-');

				if(index > 0 && index < code.Length - 1)
					code = code.Substring(index + 1);

				return new Certificate(code, file.Name, "RSA", privateKey);
			}
		}
		#endregion
	}
}
