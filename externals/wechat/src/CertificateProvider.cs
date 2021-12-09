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
		#region 构造函数
		public CertificateProvider(DirectoryInfo directory)
		{
			this.Directory = directory ?? throw new ArgumentNullException(nameof(directory));
		}

		public CertificateProvider(string path)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			this.Directory = new DirectoryInfo(path);
		}
		#endregion

		#region 公共属性
		public DirectoryInfo Directory { get; }
		#endregion

		#region 获取证书
		Certificate ICertificateProvider<Certificate>.GetCertificate(object parameter) => parameter == null ? null : this.GetCertificate(parameter.ToString());
		public Certificate GetCertificate(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			var directory = this.Directory;
			if(directory == null || !directory.Exists)
				return null;

			var directories = directory.GetDirectories(key);

			if(directories != null && directories.Length > 0)
				directory = directories[0];

			var files = directory.GetFiles(key + "*");

			if(files == null || files.Length == 0)
				files = directory.GetFiles();

			FileInfo file = null;

			for(int i = 0; i < files.Length; i++)
			{
				if(file == null)
					file = files[i];
				else if(files[i].LastWriteTimeUtc > file.LastWriteTimeUtc)
					file = files[i];
			}

			return ResolveFile(file);
		}
		#endregion

		#region 私有方法
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
						return CreateCertificate(file, data);
					}
			}

			return null;

			static Certificate CreateCertificate(FileInfo file, byte[] privateKey) => new Certificate(Path.GetFileNameWithoutExtension(file.Name), file.Name, "RSA", privateKey);
		}
		#endregion
	}
}
