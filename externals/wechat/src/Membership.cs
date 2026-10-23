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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat;

public partial class Membership
{
	#region 构造函数
	public Membership(IAuthority authority, Account account, string brandId)
	{
		ArgumentNullException.ThrowIfNull(authority);
		ValidateText(brandId, 16, nameof(brandId));
		if(account.IsEmpty || account.Type != AccountType.Channel || authority.Accounts == null ||
			!(authority.Accounts.Default == account || authority.Accounts.Any(candidate => candidate == account)))
			throw new ArgumentException(null, nameof(account));

		this.Authority = authority;
		this.Account = account;
		this.BrandId = brandId;
		this.Templates = new TemplateService(this);
	}
	#endregion

	#region 公共属性
	public IAuthority Authority { get; }
	public Account Account { get; }
	public string BrandId { get; }
	public TemplateService Templates { get; }
	#endregion

	#region 公共方法
	public async ValueTask<IReadOnlyList<Allocation>> AllocateAsync(string templateId, IEnumerable<string> codes, CancellationToken cancellation = default)
	{
		ValidateText(templateId, 32, nameof(templateId));
		ArgumentNullException.ThrowIfNull(codes);
		var values = codes.ToArray();
		var requested = new HashSet<string>(values, StringComparer.Ordinal);

		if(values.Length == 0 || requested.Count != values.Length)
			throw new ArgumentException(null, nameof(codes));

		foreach(var value in values)
			ValidateText(value, 32, nameof(codes));

		var template = await this.Templates.GetAsync(templateId, cancellation);
		if(template.CodeMode != CardMode.Manual)
			throw new InvalidOperationException();

		var result = await Paying.HttpUtility.PostAsync<object, AllocationResult>(Paying.HttpClientFactory.GetHttpClient(this.Authority),
			$"marketing/membercard-open/cards/{Uri.EscapeDataString(templateId)}/codes/deposit", new { code = values }, cancellation);
		var returned = new HashSet<string>(StringComparer.Ordinal);
		var entries = result.Data ?? [];

		if(entries.Any(entry => !requested.Contains(entry.Code) || !returned.Add(entry.Code) || string.IsNullOrEmpty(entry.Result)))
			throw new FormatException();

		return entries;
	}

	public async ValueTask ObsoleteAsync(string templateId, string code, string reason = null, CancellationToken cancellation = default)
	{
		ValidateText(templateId, 32, nameof(templateId));
		ValidateText(code, 32, nameof(code));
		if(reason != null)
			ValidateText(reason, 30, nameof(reason));

		await this.Templates.GetAsync(templateId, cancellation);
		await Paying.HttpUtility.PostAsync(Paying.HttpClientFactory.GetHttpClient(this.Authority),
			$"marketing/membercard-open/cards/{Uri.EscapeDataString(templateId)}/codes/{Uri.EscapeDataString(code)}/unavailable", new { reason }, cancellation);
	}
	#endregion

	#region 私有方法
	private static void ValidateText(string value, int maximum, string parameter)
	{
		if(string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl))
			throw new ArgumentException(null, parameter);
	}
	#endregion

	#region 嵌套结构
	public readonly struct Allocation
	{
		[JsonPropertyName("code")]
		public string Code { get; init; }
		[JsonPropertyName("result")]
		public string Result { get; init; }
	}

	private struct AllocationResult
	{
		[JsonPropertyName("data")]
		public Allocation[] Data { get; set; }
	}
	#endregion
}
