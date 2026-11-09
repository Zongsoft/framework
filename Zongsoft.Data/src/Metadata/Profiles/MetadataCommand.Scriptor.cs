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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
