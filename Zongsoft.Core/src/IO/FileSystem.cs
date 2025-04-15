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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.IO;

public static class FileSystem
{
	#region 成员字段
	private static readonly IFile _file;
	private static readonly IDirectory _directory;
	private static readonly FileSystemCollection _providers;
	private static string _scheme;
	#endregion

	#region 构造函数
	static FileSystem()
	{
		_file = new FileProvider();
		_directory = new DirectoryProvider();
		_providers = new FileSystemCollection();

		//默认将本地文件系统加入到提供程序集合中
		_providers.Add(LocalFileSystem.Instance);
	}
	#endregion

	#region 公共属性
	/// <summary>获取文件服务。</summary>
	public static IFile File => _file;

	/// <summary>获取目录服务。</summary>
	public static IDirectory Directory => _directory;

	/// <summary>获取文件系统提供程序集。</summary>
	public static ICollection<IFileSystem> Providers => _providers;

	/// <summary>获取文件系统的默认方案。</summary>
	public static string Scheme
	{
		get => _scheme ?? LocalFileSystem.Instance.Scheme;
		set => _scheme = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}
	#endregion

	#region 公共方法
	public static string GetUrl(string virtualPath)
	{
		if(string.IsNullOrWhiteSpace(virtualPath))
			return virtualPath;

		//如果传入的虚拟路径参数是一个URI格式，则直接返回它作为结果
		if(Zongsoft.Text.TextRegular.Uri.Url.Match(virtualPath))
			return virtualPath;

		return GetFileSystem(virtualPath, false, out Path path)?.GetUrl(path);
	}
	#endregion

	#region 私有方法
	private static IFileSystem GetFileSystem(string text, bool throwException, out Path path)
	{
		if(throwException)
			path = Path.Parse(text);
		else if(!Path.TryParse(text, out path))
			return null;

		var scheme = path.Scheme;

		//如果路径模式为空则使用默认文件系统方案
		if(string.IsNullOrEmpty(scheme))
		{
			if(path.Anchor == PathAnchor.Application)
				return LocalFileSystem.Instance;

			scheme = FileSystem.Scheme;

			//如果文件系统模式为空则返回本地文件方案
			if(string.IsNullOrEmpty(scheme))
				return LocalFileSystem.Instance;
		}

		//尝试获取对应的文件系统提供程序
		_providers.TryGetValue(scheme, out var fileSystem);

		if(fileSystem == null)
		{
			if(throwException)
				throw new IOException($"Can not obtain the File or Directory provider by the '{text}'.");

			return null;
		}

		return fileSystem;
	}

	private static IFile GetFileProvider(string text, out Path path) => GetFileSystem(text, true, out path)?.File;
	private static IDirectory GetDirectoryProvider(string text, out Path path) => GetFileSystem(text, true, out path)?.Directory;

	private static IFile[] GetFileProviders(string[] texts, out Path[] paths)
	{
		if(texts == null || texts.Length == 0)
			throw new ArgumentNullException(nameof(texts));

		paths = new Path[texts.Length];
		var result = new IFile[texts.Length];

		for(int i = 0; i < texts.Length; i++)
		{
			result[i] = GetFileProvider(texts[i], out paths[i]);
		}

		return result;
	}

	private static IDirectory[] GetDirectoryProviders(string[] texts, out Path[] paths)
	{
		if(texts == null || texts.Length == 0)
			throw new ArgumentNullException(nameof(texts));

		paths = new Path[texts.Length];
		var result = new IDirectory[texts.Length];

		for(int i = 0; i < texts.Length; i++)
		{
			result[i] = GetDirectoryProvider(texts[i], out paths[i]);
		}

		return result;
	}
	#endregion

	#region 嵌套子类
	private class DirectoryProvider : IDirectory
	{
		public bool Create(string virtualPath, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.Create(path.FullPath, properties);
		}

		public ValueTask<bool> CreateAsync(string virtualPath, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.CreateAsync(path.FullPath, properties);
		}

		public bool Delete(string virtualPath, bool recursive = false)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return false;

			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.Delete(path.FullPath, recursive);
		}

		public ValueTask<bool> DeleteAsync(string virtualPath, bool recursive)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return ValueTask.FromResult(false);

			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.DeleteAsync(path.FullPath, recursive);
		}

		public void Move(string source, string destination)
		{
			var services = FileSystem.GetDirectoryProviders([source, destination], out var paths);

			if(!string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException();

			services[0].Move(paths[0].FullPath, paths[1].FullPath);
		}

		public ValueTask MoveAsync(string source, string destination)
		{
			var services = FileSystem.GetDirectoryProviders([source, destination], out var paths);

			if(!string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException();

			return services[0].MoveAsync(paths[0].FullPath, paths[1].FullPath);
		}

		public bool Exists(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return false;

			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.Exists(path.FullPath);
		}

		public ValueTask<bool> ExistsAsync(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return ValueTask.FromResult(false);

			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.ExistsAsync(path.FullPath);
		}

		public DirectoryInfo GetInfo(string virtualPath)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetInfo(path.FullPath);
		}

		public ValueTask<DirectoryInfo> GetInfoAsync(string virtualPath)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetInfoAsync(path.FullPath);
		}

		public bool SetInfo(string virtualPath, IDictionary<string, object> properties)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.SetInfo(path.FullPath, properties);
		}

		public ValueTask<bool> SetInfoAsync(string virtualPath, IDictionary<string, object> properties)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.SetInfoAsync(path.FullPath, properties);
		}

		public IEnumerable<PathInfo> GetChildren(string virtualPath) => this.GetChildren(virtualPath, null, false);
		public IEnumerable<PathInfo> GetChildren(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetChildren(path.FullPath, pattern, recursive);
		}

		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string virtualPath) => this.GetChildrenAsync(virtualPath, null, false);
		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetChildrenAsync(path.FullPath, pattern, recursive);
		}

		public IEnumerable<DirectoryInfo> GetDirectories(string virtualPath) => this.GetDirectories(virtualPath, null, false);
		public IEnumerable<DirectoryInfo> GetDirectories(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetDirectories(path.FullPath, pattern, recursive);
		}

		public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string virtualPath) => this.GetDirectoriesAsync(virtualPath, null, false);
		public IAsyncEnumerable<DirectoryInfo> GetDirectoriesAsync(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetDirectoriesAsync(path.FullPath, pattern, recursive);
		}

		public IEnumerable<FileInfo> GetFiles(string virtualPath) => this.GetFiles(virtualPath, null, false);
		public IEnumerable<FileInfo> GetFiles(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetFiles(path.FullPath, pattern, recursive);
		}

		public IAsyncEnumerable<FileInfo> GetFilesAsync(string virtualPath) => this.GetFilesAsync(virtualPath, null, false);
		public IAsyncEnumerable<FileInfo> GetFilesAsync(string virtualPath, string pattern, bool recursive = false)
		{
			var service = FileSystem.GetDirectoryProvider(virtualPath, out var path);
			return service.GetFilesAsync(path.FullPath, pattern, recursive);
		}
	}

	private class FileProvider : IFile
	{
		private const int BUFFERSIZE = 64 * 1024;

		public bool Delete(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return false;

			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Delete(path.FullPath);
		}

		public ValueTask<bool> DeleteAsync(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return ValueTask.FromResult(false);

			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.DeleteAsync(path.FullPath);
		}

		public bool Exists(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return false;

			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Exists(path.FullPath);
		}

		public ValueTask<bool> ExistsAsync(string virtualPath)
		{
			if(string.IsNullOrWhiteSpace(virtualPath))
				return ValueTask.FromResult(false);

			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.ExistsAsync(path.FullPath);
		}

		public void Copy(string source, string destination) => this.Copy(source, destination, true);
		public void Copy(string source, string destination, bool overwrite)
		{
			var services = FileSystem.GetFileProviders([source, destination], out var paths);

			if(string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				services[0].Copy(paths[0].FullPath, paths[1].FullPath, overwrite);
			else
				CopyFile(services[0], paths[0].FullPath, services[1], paths[1].FullPath, overwrite);
		}

		public ValueTask CopyAsync(string source, string destination) => this.CopyAsync(source, destination, true);
		public ValueTask CopyAsync(string source, string destination, bool overwrite)
		{
			var services = FileSystem.GetFileProviders([source, destination], out var paths);

			if(string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				return services[0].CopyAsync(paths[0].FullPath, paths[1].FullPath, overwrite);
			else
				return CopyFileAsync(services[0], paths[0].FullPath, services[1], paths[1].FullPath, overwrite);
		}

		public void Move(string source, string destination)
		{
			var services = FileSystem.GetFileProviders([source, destination], out var paths);

			if(string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				services[0].Move(paths[0].FullPath, paths[1].FullPath);
			else
			{
				CopyFile(services[0], paths[0].FullPath, services[1], paths[1].FullPath, false);
				services[0].Delete(paths[0].FullPath);
			}
		}

		public async ValueTask MoveAsync(string source, string destination)
		{
			var services = FileSystem.GetFileProviders([source, destination], out var paths);

			if(string.Equals(paths[0].Scheme, paths[1].Scheme, StringComparison.OrdinalIgnoreCase))
				await services[0].MoveAsync(paths[0].FullPath, paths[1].FullPath);
			else
			{
				await CopyFileAsync(services[0], paths[0].FullPath, services[1], paths[1].FullPath, false);
				services[0].Delete(paths[0].FullPath);
			}
		}

		public FileInfo GetInfo(string virtualPath)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.GetInfo(path.FullPath);
		}

		public ValueTask<FileInfo> GetInfoAsync(string virtualPath)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.GetInfoAsync(path.FullPath);
		}

		public bool SetInfo(string virtualPath, IDictionary<string, object> properties)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.SetInfo(path.FullPath, properties);
		}

		public ValueTask<bool> SetInfoAsync(string virtualPath, IDictionary<string, object> properties)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.SetInfoAsync(path.FullPath, properties);
		}

		public Stream Open(string virtualPath, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Open(path.FullPath, properties);
		}

		public Stream Open(string virtualPath, FileMode mode, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Open(path.FullPath, mode, properties);
		}

		public Stream Open(string virtualPath, FileMode mode, FileAccess access, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Open(path.FullPath, mode, access, properties);
		}

		public Stream Open(string virtualPath, FileMode mode, FileAccess access, System.IO.FileShare share, IDictionary<string, object> properties = null)
		{
			var service = FileSystem.GetFileProvider(virtualPath, out var path);
			return service.Open(path.FullPath, mode, access, share, properties);
		}

		private static void CopyFile(IFile source, string sourcePath, IFile destination, string destinationPath, bool overwrite)
		{
			using var sourceStream = source.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var destinationStream = destination.Open(destinationPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);

			var buffer = new byte[BUFFERSIZE];
			int bytesRead;

			while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				destinationStream.Write(buffer, 0, bytesRead);
			}
		}

		private static async ValueTask CopyFileAsync(IFile source, string sourcePath, IFile destination, string destinationPath, bool overwrite)
		{
			using var sourceStream = source.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var destinationStream = destination.Open(destinationPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);

			var buffer = new byte[BUFFERSIZE];
			int bytesRead;

			while((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
			{
				await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead));
			}
		}
	}
	#endregion
}
