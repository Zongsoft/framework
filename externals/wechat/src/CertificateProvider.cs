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

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat
{
	public class CertificateProvider : ICertificateProvider<Certificate>
	{
		#region 单例字段
		public static readonly CertificateProvider Default = new CertificateProvider();
		#endregion

		#region 构造函数
		public CertificateProvider()
		{
			this.DirectoryPath = Path.Combine(this.GetType().Assembly.Location, "certificates");
		}
		#endregion

		#region 公共属性
		public string DirectoryPath { get; set; }
		#endregion

		#region 获取证书
		Certificate ICertificateProvider<Certificate>.GetCertificate(object parameter) => parameter == null ? null : this.GetCertificate(parameter.ToString());
		public Certificate GetCertificate(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			var directory = Path.Combine(this.DirectoryPath, key);
			if(!Directory.Exists(directory))
				return null;

			var files = Directory.GetFiles(directory);
			var filePath = string.Empty;
			var latest = DateTime.MinValue;

			for(int i = 0; i < files.Length; i++)
			{
				var timestamp = File.GetLastWriteTimeUtc(files[i]);

				if(timestamp > latest)
				{
					latest = timestamp;
					filePath = files[i];
				}
			}

			return ResolveFile(filePath);
		}
		#endregion

		#region 私有方法
		private static Certificate ResolveFile(string filePath)
		{
			if(string.IsNullOrEmpty(filePath))
				return null;

			var extension = Path.GetExtension(filePath);

			if(string.IsNullOrEmpty(extension))
				return null;

			switch(extension.ToLowerInvariant())
			{
				case ".bin":
				case ".key":
					using(var stream = File.OpenRead(filePath))
					{
						using var memory = new MemoryStream((int)stream.Length);
						stream.CopyTo(memory);
						return CreateCertificate(filePath, memory.ToArray());
					}
				case ".txt":
				case ".base64":
					using(var stream = File.OpenRead(filePath))
					{
						using var reader = new StreamReader(stream);
						var data = Convert.FromBase64String(reader.ReadToEnd());
						return CreateCertificate(filePath, data);
					}
				case ".pem":
					using(var stream = File.OpenRead(filePath))
					{
						using var reader = new StreamReader(stream);
						var text = new System.Text.StringBuilder((int)stream.Length);
						var line = string.Empty;

						do
						{
							line = reader.ReadLine();

							if(line.StartsWith("-----BEGIN "))
							{
								text.Append(line);
								break;
							}
						}while(line != null && !line.StartsWith("-----END "));

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
						return CreateCertificate(filePath, data);
					}
			}

			return null;

			static Certificate CreateCertificate(string filePath, byte[] privateKey) => new Certificate(Path.GetFileNameWithoutExtension(filePath), Path.GetFileName(filePath), "RSA", privateKey);
		}
		#endregion
	}
}
