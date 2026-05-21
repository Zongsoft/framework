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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示当前机器的硬件配置档案。
/// </summary>
public class HardwareProfile : IReadOnlyCollection<IHardware>
{
	#region 常量定义
	private const char CATEGORY_SEPARATOR = '/';
	#endregion

	#region 成员字段
	private readonly IHardware[] _hardwares;
	private Dictionary<string, IHardware> _mapping;
	#endregion

	#region 构造函数
	/// <summary>初始化 <see cref="HardwareProfile"/> 类的新实例。</summary>
	/// <param name="hardwares">组成档案的硬件设备集。</param>
	public HardwareProfile(IEnumerable<IHardware> hardwares)
	{
		_hardwares = hardwares == null ? [] : hardwares.Where(hardware => hardware != null).ToArray();

		this.Mainboard = _hardwares.FirstOrDefault(IsMainboard);
		this.Processors = [.. _hardwares.Where(IsProcessor)];
		this.Memories = [.. _hardwares.Where(IsMemory)];
		this.Storages = [.. _hardwares.Where(IsStorage)];
		this.Networks = [.. _hardwares.Where(IsNetwork)];
		this.Devices = [.. _hardwares.Where(hardware =>
			!IsMainboard(hardware) &&
			!IsMemory(hardware) &&
			!IsStorage(hardware) &&
			!IsNetwork(hardware) &&
			!IsProcessor(hardware))
		];
	}
	#endregion

	#region 静态属性
	/// <summary>获取本机的硬件信息。</summary>
	public static HardwareProfile Current
	{
		get
		{
			if(field != null)
				return field;

			try
			{
				field ??= Load();
			}
			catch { }

			return field;
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取配置的硬件数量。</summary>
	public int Count => _hardwares.Length;

	/// <summary>获取配置的唯一标识。</summary>
	public string Identifier
	{
		get
		{
			if(string.IsNullOrEmpty(field))
			{
				if(this.Count == 0)
					return null;

				var data = new List<byte>();

				if(this.Mainboard != null && this.Mainboard.HasUnique(out var id))
					data.AddRange(System.Text.Encoding.UTF8.GetBytes(id));

				foreach(var processor in this.Processors)
				{
					if(processor.HasUnique(out id))
						data.AddRange(System.Text.Encoding.UTF8.GetBytes(id));
				}

				foreach(var fireware in this.Find("BIOS"))
				{
					if(fireware.HasUnique(out id))
						data.AddRange(System.Text.Encoding.UTF8.GetBytes(id));
				}

				if(data != null && data.Count > 0)
					field = Convert.ToHexString(System.Security.Cryptography.MD5.HashData([..data]));
			}

			return field;
		}
	}

	/// <summary>获取主板设备。</summary>
	public IHardware Mainboard { get; }

	/// <summary>获取其他设备集。</summary>
	public IReadOnlyList<IHardware> Devices { get; }

	/// <summary>获取网络设备集。</summary>
	public IReadOnlyList<IHardware> Networks { get; }

	/// <summary>获取内存设备集。</summary>
	public IReadOnlyList<IHardware> Memories { get; }

	/// <summary>获取存储设备集。</summary>
	public IReadOnlyList<IHardware> Storages { get; }

	/// <summary>获取处理器设备集。</summary>
	public IReadOnlyList<IHardware> Processors { get; }

	/// <summary>根据唯一编号或硬件代码获取指定硬件。</summary>
	/// <param name="id">硬件唯一编号或硬件代码。</param>
	/// <returns>如果找到则返回对应的硬件设备，否则返回空(<c>null</c>)。</returns>
	/// <remarks>查找时优先匹配硬件的唯一编号，如果没有匹配项则再匹配硬件代码。</remarks>
	public IHardware this[string id]
	{
		get
		{
			if(string.IsNullOrEmpty(id))
				return null;

			if(_mapping == null)
			{
				lock(_hardwares)
				{
					if(_mapping != null)
						return _mapping.TryGetValue(id, out var value) ? value : null;

					var mapping = new Dictionary<string, IHardware>(StringComparer.OrdinalIgnoreCase);

					foreach(var hardware in _hardwares)
					{
						if(hardware.HasUnique(out var identifier))
							mapping[identifier] = hardware;
					}

					foreach(var hardware in _hardwares)
					{
						if(!string.IsNullOrWhiteSpace(hardware.Code))
							mapping.TryAdd(hardware.Code, hardware);
					}

					_mapping = mapping;
				}
			}

			return _mapping.TryGetValue(id, out var found) ? found : null;
		}
	}
	#endregion

	#region 公共方法
	/// <summary>根据硬件分类查找硬件设备。</summary>
	/// <param name="path">硬件分类路径，多级分类以斜杠(<c>/</c>)分隔。</param>
	/// <returns>返回指定分类及其子分类下的硬件设备集。</returns>
	public IEnumerable<IHardware> Find(string path)
	{
		var category = Normalize(path);

		if(string.IsNullOrEmpty(category))
			return _hardwares;

		return _hardwares.Where(hardware => IsCategory(hardware?.Category, category));
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Identifier}({this.Count})";
	#endregion

	#region 静态方法
	/// <summary>从指定服务容器加载硬件配置档案。</summary>
	/// <param name="services">提供硬件采集器的服务容器。</param>
	/// <returns>返回加载得到的硬件配置档案。</returns>
	public static HardwareProfile Load(IServiceProvider services = null)
	{
		if(services == null)
			services = Services.ApplicationContext.Current?.Services ?? throw new ArgumentNullException(nameof(services));

		return new HardwareProfile(Collect(services));

		static IEnumerable<IHardware> Collect(IServiceProvider serviceProvider)
		{
			foreach(var collector in serviceProvider.GetServices<IHardwareCollector>())
			{
				var hardwares = collector.Collect();

				if(hardwares == null)
					continue;

				foreach(var hardware in hardwares)
				{
					if(hardware != null)
						yield return hardware;
				}
			}
		}
	}

	/// <summary>从当前应用服务容器异步加载硬件配置档案。</summary>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回加载得到的硬件配置档案。</returns>
	public static ValueTask<HardwareProfile> LoadAsync(CancellationToken cancellation = default) => LoadAsync(null, cancellation);

	/// <summary>从指定服务容器异步加载硬件配置档案。</summary>
	/// <param name="services">提供硬件采集器的服务容器。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回加载得到的硬件配置档案。</returns>
	public static async ValueTask<HardwareProfile> LoadAsync(IServiceProvider services, CancellationToken cancellation = default)
	{
		if(services == null)
			services = Services.ApplicationContext.Current?.Services ?? throw new ArgumentNullException(nameof(services));

		var hardwares = new List<IHardware>();

		foreach(var collector in services.GetServices<IHardwareCollector>())
		{
			cancellation.ThrowIfCancellationRequested();

			var items = collector.CollectAsync(cancellation);
			if(items == null)
				continue;

			await foreach(var hardware in items.WithCancellation(cancellation))
			{
				if(hardware != null)
					hardwares.Add(hardware);
			}
		}

		return new HardwareProfile(hardwares);
	}
	#endregion

	#region 私有方法
	private static bool IsMainboard(IHardware hardware) => IsCategory(hardware?.Category, "mainboard");
	private static bool IsProcessor(IHardware hardware) => IsCategory(hardware?.Category, "processor") || IsCategory(hardware?.Category, "processors");
	private static bool IsMemory(IHardware hardware) => IsCategory(hardware?.Category, "memory") || IsCategory(hardware?.Category, "memories");
	private static bool IsStorage(IHardware hardware) => IsCategory(hardware?.Category, "storage") || IsCategory(hardware?.Category, "storages");
	private static bool IsNetwork(IHardware hardware) => IsCategory(hardware?.Category, "network") || IsCategory(hardware?.Category, "networks");

	private static bool IsCategory(string category, string path)
	{
		category = Normalize(category);
		path = Normalize(path);

		if(string.IsNullOrEmpty(path))
			return true;

		return string.Equals(category, path, StringComparison.OrdinalIgnoreCase) ||
		       category.StartsWith(path + CATEGORY_SEPARATOR, StringComparison.OrdinalIgnoreCase);
	}

	private static string Normalize(string path)
	{
		if(string.IsNullOrWhiteSpace(path))
			return string.Empty;

		return string.Join(CATEGORY_SEPARATOR, path.Split(CATEGORY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IHardware> GetEnumerator()
	{
		for(int i = 0; i < _hardwares.Length; i++)
			yield return _hardwares[i];
	}
	#endregion
}
