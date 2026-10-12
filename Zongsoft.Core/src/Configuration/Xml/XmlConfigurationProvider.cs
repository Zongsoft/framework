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
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Zongsoft.Configuration.Xml;

/// <summary>加载 XML 配置文件、执行处理指令并跟踪导入文件的变化。</summary>
public class XmlConfigurationProvider : FileConfigurationProvider
{
	#region 成员字段
	private readonly XmlConfigurationOptions _options;
	private readonly FileProvider _fileProvider;
	#endregion

	#region 构造函数
	/// <summary>初始化 <see cref="XmlConfigurationProvider"/> 类的新实例。</summary>
	/// <param name="source">XML 配置来源及其加载选项。</param>
	public XmlConfigurationProvider(XmlConfigurationSource source) : base(Prepare(source))
	{
		_options = source.Options;
		_fileProvider = this.Source.FileProvider as FileProvider;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前配置数据对应的处理指令只读快照，按导入位置展开并保留重复声明。</summary>
	/// <remarks>遵循文件配置基类的数据生命周期；基类清空配置数据时，此集合同时为空。</remarks>
	public IReadOnlyList<XmlConfigurationInstruction> Instructions => XmlStreamConfigurationProvider.GetInstructions(this.Data);
	#endregion

	#region 重写方法
	/// <summary>读取 XML 配置并执行处理指令，成功后替换当前快照。</summary>
	/// <param name="stream">配置输入流；此方法不会关闭它。</param>
	public override void Load(Stream stream)
	{
		IDictionary<string, string> data;
		var session = new XmlConfigurationSession(_options);

		try
		{
			var source = XmlConfigurationSession.Source.Create(stream, _fileProvider?.Provider ?? this.Source.FileProvider, this.Source.Path);
			data = session.Read(stream, source);
		}
		finally
		{
			//失败时也保留本次已经发现的依赖，以便缺失文件补齐或错误修复后恢复加载
			_fileProvider?.UpdateDependencies(session.Dependencies);
		}

		this.Data = data;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if(disposing)
			_fileProvider?.Dispose();
	}
	#endregion

	#region 私有方法
	private static XmlConfigurationSource Prepare(XmlConfigurationSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		if(!source.ReloadOnChange || source.FileProvider == null)
			return source;

		//每个提供程序独享依赖通知，不修改可被其它构建器复用的来源或调用方的文件提供器
		return new XmlConfigurationSource(source.Options)
		{
			Path = source.Path,
			Optional = source.Optional,
			ReloadOnChange = source.ReloadOnChange,
			ReloadDelay = source.ReloadDelay,
			OnLoadException = source.OnLoadException,
			FileProvider = new FileProvider(source.FileProvider),
		};
	}
	#endregion

	#region 嵌套子类
	//将有效导入的通知接到根文件的变化令牌，仍由 FileConfigurationProvider 完成重载及异常处理。
	private sealed class FileProvider(IFileProvider provider) : IFileProvider, IDisposable
	{
		#region 成员字段
		private readonly object _sync = new();
		private readonly Dictionary<XmlConfigurationSession.Source, Subscription> _subscriptions = [];
		private CancellationTokenSource _changeTokenSource = new();
		private bool _disposed;
		#endregion

		#region 公共属性
		public IFileProvider Provider { get; } = provider;
		#endregion

		#region 公共方法
		public IDirectoryContents GetDirectoryContents(string subpath) => this.Provider.GetDirectoryContents(subpath);
		public IFileInfo GetFileInfo(string subpath)
		{
			var file = this.Provider.GetFileInfo(subpath);
			if(!file.Exists)
				this.UpdateDependencies([]);

			return file;
		}

		public IChangeToken Watch(string filter)
		{
			var rootToken = this.Provider.Watch(filter);

			lock(_sync)
				return _disposed ? NullChangeToken.Singleton : new CompositeChangeToken([rootToken, new CancellationChangeToken(_changeTokenSource.Token)]);
		}
		#endregion

		#region 内部方法
		internal void UpdateDependencies(HashSet<XmlConfigurationSession.Source> dependencies)
		{
			List<Subscription> removedSubscriptions = [];
			List<XmlConfigurationSession.Source> addedSources;

			lock(_sync)
			{
				if(_disposed)
					return;

				foreach(var entry in _subscriptions.ToArray())
				{
					if(!dependencies.Contains(entry.Key))
					{
						_subscriptions.Remove(entry.Key);
						removedSubscriptions.Add(entry.Value);
					}
				}

				addedSources = dependencies.Where(source => !_subscriptions.ContainsKey(source)).ToList();
			}

			//取消订阅可能等待正在执行的回调，不在内部锁中释放。
			foreach(var subscription in removedSubscriptions)
				subscription.Dispose();

			foreach(var source in addedSources)
			{
				var subscription = new Subscription(source, this.Provider, Signal);

				lock(_sync)
				{
					if(!_disposed && _subscriptions.TryAdd(source, subscription))
						continue;
				}

				subscription.Dispose();
			}

			void Signal()
			{
				CancellationTokenSource previous;

				lock(_sync)
				{
					if(_disposed)
						return;

					previous = _changeTokenSource;
					_changeTokenSource = new();
				}

				try
				{
					previous.Cancel();
				}
				finally
				{
					previous.Dispose();
				}
			}
		}
		#endregion

		#region 处置释放
		public void Dispose()
		{
			Subscription[] subscriptions;
			CancellationTokenSource changeTokenSource;

			lock(_sync)
			{
				if(_disposed)
					return;
				_disposed = true;
				subscriptions = _subscriptions.Values.ToArray();
				_subscriptions.Clear();
				changeTokenSource = _changeTokenSource;
			}

			foreach(var subscription in subscriptions)
				subscription.Dispose();

			//基类先解除加载回调，再触发组合令牌释放对根文件令牌的内部订阅。
			try
			{
				changeTokenSource.Cancel();
			}
			finally
			{
				changeTokenSource.Dispose();
			}
		}
		#endregion

		#region 嵌套子类
		private sealed class Subscription : IDisposable
		{
			private readonly PhysicalFileProvider _ownedProvider;
			private readonly IDisposable _registration;

			public Subscription(XmlConfigurationSession.Source source, IFileProvider rootProvider, Action onChanged)
			{
				var provider = source.Provider;
				var path = source.Path;

				if(provider == null && rootProvider is PhysicalFileProvider physicalProvider)
				{
					var relativePath = Path.GetRelativePath(physicalProvider.Root, path).Replace('\\', '/');
					if(!Path.IsPathRooted(relativePath) && relativePath != ".." && !relativePath.StartsWith("../", StringComparison.Ordinal))
					{
						provider = physicalProvider;
						path = relativePath;
					}
				}

				if(provider == null)
				{
					var directory = Path.GetDirectoryName(path);
					while(!Directory.Exists(directory))
					{
						directory = Path.GetDirectoryName(directory);
						if(string.IsNullOrEmpty(directory))
							return;
					}

					_ownedProvider = new PhysicalFileProvider(directory);
					provider = _ownedProvider;
					path = Path.GetRelativePath(directory, path).Replace('\\', '/');
				}

				try
				{
					_registration = ChangeToken.OnChange(() => provider.Watch(path), onChanged);
				}
				catch
				{
					_ownedProvider?.Dispose();
					throw;
				}
			}

			public void Dispose()
			{
				_registration?.Dispose();
				_ownedProvider?.Dispose();
			}
		}
		#endregion
	}
	#endregion
}
