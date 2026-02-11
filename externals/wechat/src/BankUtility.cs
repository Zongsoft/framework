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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Zongsoft.Data;

namespace Zongsoft.Externals.Wechat
{
	public static class BankUtility
	{
		public static async ValueTask<IEnumerable<Bank>> GetBanksAsync(this IAuthority authority, BankKind kind, Paging page = null, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(!page.IsLimited(out var limit, out var offset))
			{
				if(page.IsPaged(out var index, out var size))
				{
					limit = size;
					offset = (index - 1) * size;
				}
				else
				{
					limit = 100;
					offset = 0;
				}
			}

			var response = kind == BankKind.Personal ?
				await Paying.HttpClientFactory.GetHttpClient(authority.Certificate).GetAsync($"capital/capitallhh/banks/personal-banking?offset={offset}&limit={limit}", cancellation) :
				await Paying.HttpClientFactory.GetHttpClient(authority.Certificate).GetAsync($"capital/capitallhh/banks/corporate-banking?offset={offset}&limit={limit}", cancellation);

			var result = await response.GetResultAsync<Result<Bank>>(cancellation);

			if(result.HasData)
			{
				if(page != null)
					page.Total = result.Total;

				return result.Data;
			}

			return null;
		}

		public static async ValueTask<IEnumerable<BankBranch>> GetBranchesAsync(this IAuthority authority, string id, string city, Paging page = null, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(!page.IsLimited(out var limit, out var offset))
			{
				if(page.IsPaged(out var index, out var size))
				{
					limit = size;
					offset = (index - 1) * size;
				}
				else
				{
					limit = 100;
					offset = 0;
				}
			}

			var response = await Paying.HttpClientFactory.GetHttpClient(authority.Certificate).GetAsync($"capital/capitallhh/banks/{id}/branches?city_code={city}&offset={offset}&limit={limit}", cancellation);
			var result = await response.GetResultAsync<Result<BankBranch>>(cancellation);

			if(result.HasData)
			{
				if(page != null)
					page.Total = result.Total;

				return result.Data;
			}

			return null;
		}

		public static async ValueTask<Bank?> FindAsync(this IAuthority authority, string code, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(string.IsNullOrEmpty(code))
				throw new ArgumentNullException(nameof(code));

			var account = Convert.ToBase64String(authority.Certificate.Encrypt(code));
			var response = await Paying.HttpClientFactory.GetHttpClient(authority.Certificate).GetAsync($"capital/capitallhh/banks/search-banks-by-bank-account?account_number={account}", cancellation);
			var result = await Paying.HttpUtility.GetResultAsync<Result<Bank>>(response, cancellation);

			return result.HasData ? result.Data[0] : null;
		}

		private struct Result<T>
		{
			[JsonPropertyName("total_count")]
			public int Total { get; set; }
			[JsonPropertyName("count")]
			public int Count { get; set; }
			[JsonPropertyName("offset")]
			public int Offset { get; set; }
			[JsonPropertyName("data")]
			public T[] Data { get; set; }

			[JsonIgnore]
			[Serialization.SerializationMember(Ignored = true)]
			public bool HasData => this.Data != null && this.Data.Length > 0;
		}

		public enum BankKind
		{
			Personal,
			Corporational,
		}

		public struct Bank
		{
			[JsonPropertyName("account_bank_code")]
			public uint Id { get; set; }

			[JsonPropertyName("account_bank")]
			public string Name { get; set; }

			[JsonPropertyName("bank_alias_code")]
			public string Code { get; set; }

			[JsonPropertyName("bank_alias")]
			public string Alias { get; set; }

			[JsonPropertyName("need_bank_branch")]
			public bool Branched { get; set; }
		}

		public struct BankBranch
		{
			[JsonPropertyName("bank_branch_id")]
			public string Code { get; set; }
			[JsonPropertyName("bank_branch_name")]
			public string Name { get; set; }
		}
	}
}
