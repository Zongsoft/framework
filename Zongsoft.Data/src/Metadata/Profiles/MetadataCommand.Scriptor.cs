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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace Zongsoft.Data.Metadata.Profiles;

partial class MetadataCommand
{
	public class MetadataCommandScriptor(IDataCommand command) : DataCommandScriptor(command)
	{
		internal void Load(string directory)
		{
			if(string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
				return;

			directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));

			//Load the legacy unqualified scripts first, then let qualified scripts override them.
			LoadFiles(this.Command.Name);

			if(!string.Equals(this.Command.Name, this.Command.QualifiedName, StringComparison.OrdinalIgnoreCase))
				LoadFiles(this.Command.QualifiedName);

			void LoadFiles(string name)
			{
				var files = Directory.GetFiles(directory, $"{name}-*.sql", SearchOption.AllDirectories);
				for(int i = 0; i < files.Length; i++)
					LoadFile(files[i], false);

				files = Directory.GetFiles(directory, $"{name}.sql", SearchOption.AllDirectories);
				for(int i = 0; i < files.Length; i++)
					LoadFile(files[i], true);
			}

			bool LoadFile(string filePath, bool directoryNamed)
			{
				var driver = string.Empty;
				var index = -1;

				if(directoryNamed)
				{
					var parent = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(filePath));

					if(string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase))
						return false;

					driver = Path.GetFileName(parent);

					if(string.Equals(driver, "scripts", StringComparison.OrdinalIgnoreCase))
						return false;

					index = driver.LastIndexOf('-');

					if(index > 0)
						driver = driver[(index + 1)..];
				}
				else
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);
					index = fileName.LastIndexOf('-');

					if(index > 0)
						driver = fileName[(index + 1)..];
				}

				return !string.IsNullOrEmpty(driver) && this.SetScript(driver, File.ReadAllText(filePath));
			}
		}
	}
}
