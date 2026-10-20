using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using Xunit;

namespace Zongsoft.Configuration.Tests;

internal sealed class XmlImportFiles : IFileProvider, IDisposable
{
	private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _physicalPaths = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _reads = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Exception> _errors = new(StringComparer.Ordinal);
	private readonly List<WatchToken> _tokens = [];

	public string[] ActivePaths => [.. _tokens.Where(token => token.ActiveCount > 0 && !token.HasChanged).Select(token => token.Path).Distinct().OrderBy(path => path)];
	public int GetReadCount(string path) => _reads.GetValueOrDefault(path);
	public void Set(string path, string content) => _files[path] = content;
	public void SetPhysicalPath(string path, string physicalPath) => _physicalPaths[path] = physicalPath;
	public void FailOpening(string path, Exception error) => _errors[path] = error;

	public void Change(string path, string content)
	{
		this.Set(path, content);

		foreach(var token in _tokens.Where(token => token.Path == path && !token.HasChanged).ToArray())
			token.Signal();
	}

	public IFileInfo GetFileInfo(string subpath)
	{
		return new MemoryFile(subpath, _files.GetValueOrDefault(subpath), () =>
		{
			_reads[subpath] = this.GetReadCount(subpath) + 1;

			if(_errors.TryGetValue(subpath, out var error) && error != null)
				throw error;
		}, _physicalPaths.GetValueOrDefault(subpath));
	}

	public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

	public IChangeToken Watch(string filter)
	{
		var token = new WatchToken(filter);
		_tokens.Add(token);
		return token;
	}

	public void Dispose()
	{
		foreach(var token in _tokens)
			token.Dispose();
	}

	private sealed class MemoryFile(string path, string content, Action opening, string physicalPath) : IFileInfo
	{
		public bool Exists => content != null;
		public long Length => content == null ? -1 : Encoding.UTF8.GetByteCount(content);
		public string PhysicalPath => physicalPath;
		public string Name => Path.GetFileName(path);
		public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
		public bool IsDirectory => false;

		public Stream CreateReadStream()
		{
			opening();
			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}
	}

	private sealed class WatchToken(string path) : IChangeToken, IDisposable
	{
		private readonly CancellationTokenSource _signal = new();
		private int _activeRegistrations;

		public string Path => path;
		public int ActiveCount => Volatile.Read(ref _activeRegistrations);
		public bool ActiveChangeCallbacks => true;
		public bool HasChanged => _signal.IsCancellationRequested;
		public void Signal() => _signal.Cancel();
		public void Dispose() => _signal.Dispose();

		public IDisposable RegisterChangeCallback(Action<object> callback, object state)
		{
			Interlocked.Increment(ref _activeRegistrations);
			return new Registration(_signal.Token.Register(callback, state), () => Interlocked.Decrement(ref _activeRegistrations));
		}

		private sealed class Registration(IDisposable registration, Action onDisposed) : IDisposable
		{
			private int _disposed;

			public void Dispose()
			{
				if(Interlocked.Exchange(ref _disposed, 1) != 0)
					return;

				registration.Dispose();
				onDisposed();
			}
		}
	}
}

internal sealed class XmlImportPhysicalFiles : IDisposable
{
	private readonly List<string> _links = [];

	public XmlImportPhysicalFiles() => Directory.CreateDirectory(this.Root);
	public string Root { get; } = Path.Combine(Path.GetTempPath(), "zongsoft-xml-import-tests", Guid.NewGuid().ToString("N"));
	public string GetPath(string relativePath) => Path.GetFullPath(Path.Combine(this.Root, relativePath));

	public string Write(string relativePath, string content)
	{
		var path = this.GetPath(relativePath);
		Assert.StartsWith(this.Root + Path.DirectorySeparatorChar, path);
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		File.WriteAllText(path, content.Replace("\r\n", "\n").Replace("\n", "\r\n"));
		return path;
	}

	public void Link(string relativePath)
	{
		var path = this.GetPath(relativePath);
		Assert.StartsWith(this.Root + Path.DirectorySeparatorChar, path);

		if(OperatingSystem.IsWindows())
			RunPowerShell($"New-Item -ItemType Junction -Path '{Quote(path)}' -Target '{Quote(this.Root)}' -ErrorAction Stop | Out-Null");
		else
			Directory.CreateSymbolicLink(path, this.Root);

		_links.Add(path);
	}

	public void Dispose()
	{
		var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "zongsoft-xml-import-tests")) + Path.DirectorySeparatorChar;
		Assert.StartsWith(parent, Path.GetFullPath(this.Root));

		foreach(var link in _links)
		{
			Assert.StartsWith(this.Root + Path.DirectorySeparatorChar, Path.GetFullPath(link));
			Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
			Assert.Equal(this.Root, new DirectoryInfo(link).ResolveLinkTarget(true).FullName);
			Directory.Delete(link, false);
		}

		Directory.Delete(this.Root, true);
	}

	private static string Quote(string value) => value.Replace("'", "''");
	private static void RunPowerShell(string command)
	{
		var info = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
		info.ArgumentList.Add("-NoProfile");
		info.ArgumentList.Add("-NonInteractive");
		info.ArgumentList.Add("-Command");
		info.ArgumentList.Add(command);

		using var process = Process.Start(info);
		Assert.True(process.WaitForExit(10000));
		Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
	}
}
