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
using System.Collections.ObjectModel;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Aliyun.Options;

/// <summary>表示阿里云的常规配置选项。</summary>
public class GeneralOptions
{
	#region 构造函数
	public GeneralOptions()
	{
		this.Certificates = new CertificateCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置配置的服务中心。</summary>
	public ServiceCenterName Name { get; set; }

	/// <summary>获取或设置一个值，指示是否为内网访问。</summary>
	[ConfigurationProperty("intranet")]
	public bool IsIntranet { get; set; }

	/// <summary>获取阿里云的凭证提供程序。</summary>
	public ICertificateProvider Certificates { get; private set; }
	#endregion

	#region 静态方法
	private static GeneralOptions _instance;
	public static GeneralOptions Instance => _instance ?? (_instance = ApplicationContext.Current.Configuration.GetOption<Options.GeneralOptions>("Externals/Aliyun/General"));
	#endregion

	#region 嵌套子类
	private class CertificateCollection : KeyedCollection<string, ICertificate>, ICertificateProvider
	{
		public CertificateCollection() : base(StringComparer.OrdinalIgnoreCase) => _default = string.Empty;
		private string _default;
		public string Default { get => _default; set => _default = value ?? string.Empty; }
		ICertificate ICertificateProvider.Default => this.TryGetValue(_default, out var certificate) ? certificate : null;
		public ICertificate GetCertificate(string name) => this.TryGetValue(string.IsNullOrEmpty(name) ? _default : name, out var certificate) ? certificate : null;
		protected override string GetKeyForItem(ICertificate certificate) => certificate.Name;
	}
	#endregion
}
