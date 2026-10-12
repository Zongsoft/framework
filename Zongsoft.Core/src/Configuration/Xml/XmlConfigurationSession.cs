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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Collections.Generic;

using Microsoft.Extensions.FileProviders;

namespace Zongsoft.Configuration.Xml;

//一次根加载共享注册表、选项和活动导入链；每个文件独立积累配置后按引用位置合并。
internal sealed class XmlConfigurationSession
{
	#region 成员字段
	private readonly XmlConfigurationOptions _options;
	private readonly Dictionary<string, XmlConfigurationInstructionProcessor> _processors;
	private readonly HashSet<Source> _activeSources = [];
	private Source _rootSource;
	private int _depth;
	#endregion

	#region 构造函数
	internal XmlConfigurationSession(XmlConfigurationOptions options)
	{
		this.Dependencies = [];
		_processors = XmlConfiguration.Instructions.GetSnapshot();
		_options = options.Clone();
	}
	#endregion

	#region 公共属性
	public HashSet<Source> Dependencies { get; }
	#endregion

	#region 读取方法
	internal IDictionary<string, string> Read(Stream stream, Source source, string filePath = null, XmlConfigurationInstruction referer = null, string selectedOptionPath = null, int maximumDepth = XmlConfigurationInstructionOptions.ImportOptions.DEFAULT_MAXIMUM_DEPTH)
	{
		ArgumentNullException.ThrowIfNull(stream);
		var identity = source?.GetIdentity();

		if(_depth >= maximumDepth)
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_MaximumDepth_Message, referer, maximumDepth);

		if(identity != null && !_activeSources.Add(identity))
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_CircularImport_Message, referer, source.Path);

		if(_depth == 0)
			_rootSource = source;

		_depth++;

		try
		{
			var context = new XmlConfigurationContext(filePath ?? source?.Path ?? string.Empty, _depth, referer, XmlConfigurationReader.CreateData());
			var file = new File(source, context, selectedOptionPath);

			_options.Loading?.Invoke(context);
			var result = XmlConfigurationReader.Read(stream, context.FilePath, this, file);
			_options.Loaded?.Invoke(context);

			return result;
		}
		finally
		{
			_depth--;
			if(identity != null)
				_activeSources.Remove(identity);
		}
	}

	internal void Process(File file, XmlConfigurationInstruction instruction, string optionPath, int xmlDepth)
	{
		var options = _options.Instructions.Contains(instruction.Name) ? _options.Instructions[instruction.Name] :
			string.Equals(instruction.Name, XmlConfigurationInstructionOptions.ImportOptions.NAME, StringComparison.OrdinalIgnoreCase) ? XmlConfigurationInstructionOptions.Import() : new XmlConfigurationInstructionOptions(instruction.Name);

		if(options.Behavior == XmlConfigurationInstructionBehavior.Ignore)
			return;
		if(options.Behavior == XmlConfigurationInstructionBehavior.Suppress)
			throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_InstructionSuppressed_Message, instruction);

		var context = new XmlConfigurationInstructionContext(this, file, instruction, optionPath, xmlDepth, options);
		_options.Instructions.Processing?.Invoke(context);

		if(!context.Handled)
		{
			if(_processors.TryGetValue(instruction.Name, out var processor))
			{
				processor.Process(context);
				context.Handled = true;
			}
			else if(context.Behavior == XmlConfigurationInstructionBehavior.Strict)
				throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_InstructionUnknown_Message, instruction);
		}

		_options.Instructions.Processed?.Invoke(context);
	}

	internal void Import(File file, XmlConfigurationInstructionContext context, string path, bool required, int maximumDepth)
	{
		var source = Source.Resolve(file.Source, path, context.Instruction);
		if(!source.Equals(_rootSource))
			this.Dependencies.Add(source);

		Stream stream;

		try
		{
			stream = source.Open();
		}
		catch(FileNotFoundException) when(!required)
		{
			return;
		}
		catch(DirectoryNotFoundException) when(!required)
		{
			return;
		}

		using(stream)
		{
			if(source.Provider != null)
				source = Source.Create(stream, source.Provider, source.Path);

			var data = this.Read(stream, source, referer: context.Instruction, selectedOptionPath: context.OptionPath ?? file.SelectedOptionPath, maximumDepth: maximumDepth);
			foreach(var entry in data)
				file.Context.Configuration[entry.Key] = entry.Value;

			file.Instructions.AddRange(XmlConfigurationReader.GetInstructions(data));
			if(data.Count > 0)
				file.ContentVersion++;
		}
	}
	#endregion

	#region 文件状态
	internal sealed class File(Source source, XmlConfigurationContext context, string selectedOptionPath)
	{
		internal Source Source { get; } = source;
		internal XmlConfigurationContext Context { get; } = context;
		internal string SelectedOptionPath { get; } = selectedOptionPath;
		internal List<XmlConfigurationInstruction> Instructions { get; } = [];
		internal int ContentVersion { get; set; }
	}
	#endregion

	#region 文件来源
	//物理来源使用文件系统身份；虚拟来源同时保留提供器实例和规范化的逻辑路径。
	internal sealed class Source(string path, IFileProvider provider = null) : IEquatable<Source>
	{
		internal string Path { get; } = path;
		internal IFileProvider Provider { get; } = provider;
		private StringComparer PathComparer => this.Provider == null ? XmlConfigurationUtility.PathComparer : StringComparer.Ordinal;

		internal Source GetIdentity() => this.Provider == null ? new(XmlConfigurationUtility.GetFileIdentity(this.Path)) : this;
		internal static Source Create(Stream stream, IFileProvider provider = null, string path = null)
		{
			if(stream is FileStream file)
				return new(file.Name);
			if(provider == null)
				return null;

			var physicalPath = provider.GetFileInfo(path).PhysicalPath;
			return string.IsNullOrEmpty(physicalPath) ? new(Normalize(path), provider) : new(global::System.IO.Path.GetFullPath(physicalPath));
		}

		internal Stream Open()
		{
			if(this.Provider == null)
				return new FileStream(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

			var file = this.Provider.GetFileInfo(this.Path);
			if(!file.Exists)
				throw new FileNotFoundException(string.Format(Properties.Resources.Configuration_Xml_ImportFileNotFound_Message, this.Path), this.Path);

			return file.CreateReadStream();
		}

		internal static Source Resolve(Source source, string path, XmlConfigurationInstruction instruction)
		{
			if(source?.Provider != null)
			{
				if(global::System.IO.Path.IsPathRooted(path))
					throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportPathInvalid_Message, instruction, path);

				var separator = source.Path.LastIndexOf('/');
				return new(Normalize(separator < 0 ? path : source.Path[..(separator + 1)] + path, instruction), source.Provider);
			}

			if(global::System.IO.Path.IsPathFullyQualified(path))
				return new(global::System.IO.Path.GetFullPath(path));

			if(global::System.IO.Path.IsPathRooted(path))
				throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportPathInvalid_Message, instruction, path);

			if(source == null)
				throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_RelativeImportRequiresFile_Message, instruction, path);

			return new(global::System.IO.Path.GetFullPath(path, global::System.IO.Path.GetDirectoryName(source.Path)));
		}

		private static string Normalize(string path, XmlConfigurationInstruction instruction = null)
		{
			var parts = new List<string>();

			foreach(var part in (path ?? string.Empty).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
			{
				if(part == ".")
					continue;

				if(part == "..")
				{
					if(parts.Count == 0)
						throw XmlConfigurationUtility.CreateException(Properties.Resources.Configuration_Xml_ImportPathInvalid_Message, instruction, path);
					parts.RemoveAt(parts.Count - 1);
				}
				else
					parts.Add(part);
			}

			return string.Join('/', parts);
		}

		public bool Equals(Source other) => other != null && ReferenceEquals(this.Provider, other.Provider) && this.PathComparer.Equals(this.Path, other.Path);
		public override bool Equals(object obj) => obj is Source other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Provider == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this.Provider), this.PathComparer.GetHashCode(this.Path));
	}
	#endregion
}
