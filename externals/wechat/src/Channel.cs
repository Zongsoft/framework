/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	public class Channel
	{
		#region 静态变量
		private static readonly System.Security.Cryptography.SHA1 SHA1 = System.Security.Cryptography.SHA1.Create();
		#endregion

		#region 成员字段
		private volatile UserManager _users;
		private volatile ChannelAuthentication _authentication;
		#endregion

		#region 构造函数
		public Channel(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
		}
		#endregion

		#region 公共属性
		public Account Account { get; }

		public UserManager Users
		{
			get
			{
				if(_users == null)
					Interlocked.CompareExchange(ref _users, new UserManager(this.Account), null);

				return _users;
			}
		}

		public ChannelAuthentication Authentication
		{
			get
			{
				if(_authentication == null)
					Interlocked.CompareExchange(ref _authentication, new ChannelAuthentication(this.Account), null);

				return _authentication;
			}
		}
		#endregion

		#region 获取凭证
		public ValueTask<string> GetCredentialAsync(CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, cancellation);
		#endregion

		#region 计算邮戳
		public byte[] Postmark(string url, out string nonce, out long timestamp, out TimeSpan period)
		{
			var task = CredentialManager.GetTicketAsync(this.Account, "jsapi");
			var result = task.IsCompletedSuccessfully ? task.Result : task.ConfigureAwait(false).GetAwaiter().GetResult();

			if(string.IsNullOrEmpty(result.ticket))
			{
				nonce = null;
				timestamp = 0;
				period = TimeSpan.Zero;

				return null;
			}

			nonce = Zongsoft.Common.Randomizer.GenerateString(16);
			timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
			period = result.period;

			var text = $"jsapi_ticket={result.ticket}&noncestr={nonce}&timestamp={timestamp}&url={url}";
			return SHA1.ComputeHash(Encoding.UTF8.GetBytes(text));
		}
		#endregion

		#region 用户管理
		public class UserManager
		{
			#region 构造函数
			public UserManager(Account account)
			{
				if(account.IsEmpty)
					throw new ArgumentNullException(nameof(account));

				this.Account = account;
			}
			#endregion

			#region 公共属性
			public Account Account { get; }
			#endregion

			#region 公共方法
			public async ValueTask<(string bookmark, string[] identifiers)> GetIdentifiersAsync(string cursor, CancellationToken cancellation = default)
			{
				var credential = await CredentialManager.GetCredentialAsync(this.Account, cancellation);

				if(string.IsNullOrEmpty(credential))
					return default;

				var response = await CredentialManager.Http.GetAsync($"/cgi-bin/user/get?access_token={credential}&next_openid={cursor}", cancellation);
				var result = await response.GetResultAsync<IdentifierResult>(cancellation);

				return result.Succeed ?
						(result.Value.Bookmark, result.Value.Data.Values) :
						(null, Array.Empty<string>());
			}

			public async ValueTask<OperationResult<UserInfo>> GetInfoAsync(string identifier, CancellationToken cancellation = default)
			{
				var credential = await CredentialManager.GetCredentialAsync(this.Account, cancellation);

				if(string.IsNullOrEmpty(credential))
					return default;

				var response = await CredentialManager.Http.GetAsync($"/cgi-bin/user/info?access_token={credential}&openid={identifier}", cancellation);
				var result = await response.GetResultAsync<UserInfoWrapper>(cancellation);

				return result.Succeed ?
						OperationResult.Success(new UserInfo(result.Value)) :
						(OperationResult)result.Failure;
			}
			#endregion

			#region 嵌套结构
			private struct IdentifierResult
			{
				[JsonPropertyName("total")]
				[Serialization.SerializationMember("total")]
				public int Total { get; set; }

				[JsonPropertyName("count")]
				[Serialization.SerializationMember("count")]
				public int Count { get; set; }

				[JsonPropertyName("next_openid")]
				[Serialization.SerializationMember("next_openid")]
				public string Bookmark { get; set; }

				[JsonPropertyName("data")]
				[Serialization.SerializationMember("data")]
				public DataResult Data { get; set; }

				public struct DataResult
				{
					[JsonPropertyName("openid")]
					[Serialization.SerializationMember("openid")]
					public string[] Values { get; set; }
				}
			}

			internal struct UserInfoWrapper
			{
				[JsonPropertyName("subscribe")]
				[Serialization.SerializationMember("subscribe")]
				public int SubscribeId { get; set; }

				[JsonPropertyName("openid")]
				[Serialization.SerializationMember("openid")]
				public string Identifier { get; set; }

				[JsonPropertyName("nickname")]
				[Serialization.SerializationMember("nickname")]
				public string Nickname { get; set; }

				[JsonPropertyName("language")]
				[Serialization.SerializationMember("language")]
				public string Language { get; set; }

				[JsonPropertyName("headimgurl")]
				[Serialization.SerializationMember("headimgurl")]
				public string Avatar { get; set; }

				[JsonPropertyName("subscribe_time")]
				[Serialization.SerializationMember("subscribe_time")]
				public long SubscribedTime { get; set; }

				[JsonPropertyName("unionid")]
				[Serialization.SerializationMember("unionid")]
				public string UnionId { get; set; }

				[JsonPropertyName("remark")]
				[Serialization.SerializationMember("remark")]
				public string Description { get; set; }

				[JsonPropertyName("groupid")]
				[Serialization.SerializationMember("groupid")]
				public int GroupId { get; set; }

				[JsonPropertyName("tagid_list")]
				[Serialization.SerializationMember("tagid_list")]
				public uint[] Tags { get; set; }

				[JsonPropertyName("subscribe_scene")]
				[Serialization.SerializationMember("subscribe_scene")]
				public string SubscribedScene { get; set; }

				[JsonPropertyName("qr_scene")]
				[Serialization.SerializationMember("qr_scene")]
				public int QRCodeScene { get; set; }

				[JsonPropertyName("qr_scene_str")]
				[Serialization.SerializationMember("qr_scene_str")]
				public string QRCodeSceneDescription { get; set; }

			}

			public struct UserInfo
			{
				internal UserInfo(UserInfoWrapper info)
				{
					this.Subscription = new SubscriptionInfo(info.SubscribeId, info.SubscribedTime, info.SubscribedScene);
					this.Identifier = info.Identifier;
					this.Nickname = info.Nickname;
					this.Language = info.Language;
					this.Avatar = info.Avatar;
					this.UnionId = info.UnionId;
					this.GroupId = info.GroupId;
					this.Tags = info.Tags;
					this.Description = info.Description;
					this.QRCodeScene = info.QRCodeScene;
					this.QRCodeSceneDescription = info.QRCodeSceneDescription;
				}

				public string Identifier { get; }
				public string Nickname { get; }
				public string Language { get; }
				public string Avatar { get; }
				public string UnionId { get; }
				public int GroupId { get; }
				public uint[] Tags { get; }
				public SubscriptionInfo Subscription { get; }
				public string Description { get; }

				public int QRCodeScene { get; }
				public string QRCodeSceneDescription { get; }

				public struct SubscriptionInfo
				{
					public SubscriptionInfo(int id, long timestamp, string scenario)
					{
						this.Id = id;
						this.Timestamp = timestamp;
						this.Scenario = scenario;
					}

					public int Id { get; }
					public long Timestamp { get; }
					public string Scenario { get; }
				}
			}
			#endregion
		}
		#endregion
	}
}
