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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat;

public partial class Membership
{
	#region 嵌套模型
	public enum CardMode
	{
		Auto,
		Manual,
		Specified,
	}

	[Flags]
	public enum UserFlags
	{
		None = 0,
		Name = 1,
		Gender = 2,
		Birthday = 4,
		City = 8,
		Email = 16,
		Phone = 32,
		Address = 64,
	}

	public partial class CardTemplate
	{
		#region 构造函数
		public CardTemplate()
		{
			this.CodeType = "QRCODE";
		}
		#endregion

		#region 公共属性
		[JsonPropertyName("appid")]
		public string AppId { get; set; }
		[JsonPropertyName("out_request_no")]
		public string RequestNo { get; set; }
		[JsonPropertyName("code_type")]
		public string CodeType { get; set; }
		[JsonPropertyName("code_mode")]
		[JsonConverter(typeof(CardModeConverter))]
		public CardMode CodeMode { get; set; }
		[JsonPropertyName("title")]
		public string Title { get; set; }
		[JsonPropertyName("logo_url")]
		public string LogoUrl { get; set; }
		[JsonPropertyName("background_picture_url")]
		public string BackgroundUrl { get; set; }
		[JsonPropertyName("service_phone")]
		public string PhoneNumber { get; set; }
		[JsonPropertyName("description")]
		public string Description { get; set; }
		[JsonPropertyName("total_quantity")]
		public int? Quantity { get; set; }
		[JsonPropertyName("init_level")]
		public string LevelName { get; set; }
		[JsonPropertyName("need_dynamic_code")]
		public bool IsDynamicCode { get; set; }

		[JsonPropertyName("brand")]
		public BrandInfo Brand { get; set; }

		[JsonPropertyName("date_information")]
		public Validity Validation { get; set; }

		[JsonPropertyName("balance_information")]
		public BalanceInfo Balance { get; set; }

		[JsonPropertyName("user_information_form")]
		public UserInfo User { get; set; }

		[JsonPropertyName("additional_statement")]
		public AdditionalInfo Additional { get; set; }
		#endregion

		#region 嵌套结构
		public struct BrandInfo
		{
			[JsonPropertyName("brand_id")]
			public string BrandId { get; set; }
			[JsonPropertyName("display_name")]
			public string Title { get; set; }
		}

		public struct BalanceInfo
		{
			[JsonPropertyName("need_balance")]
			public bool Enabled { get; set; }
			[JsonPropertyName("balance_appid")]
			public string AppId { get; set; }
			[JsonPropertyName("balance_path")]
			public string AppUrl { get; set; }
			[JsonPropertyName("balance_url")]
			public string NavigationUrl { get; set; }
		}

		public struct UserInfo
		{
			[JsonPropertyName("common_field_list")]
			[JsonConverter(typeof(UserFlagsConverter))]
			public UserFlags Flags { get; set; }
		}

		public struct AdditionalInfo
		{
			[JsonPropertyName("title")]
			public string Title { get; set; }
			[JsonPropertyName("appid")]
			public string AppId { get; set; }
			[JsonPropertyName("path")]
			public string AppUrl { get; set; }
			[JsonPropertyName("url")]
			public string NavigationUrl { get; set; }
		}

		public struct Validity
		{
			public Validity(DateTimeOffset startTime, DateTimeOffset finalTime)
			{
				this.Type = "FIX_TIME_RANGE";
				this.StartTime = startTime;
				this.FinalTime = finalTime;
			}

			[JsonPropertyName("type")]
			public string Type { get; set; }
			[JsonPropertyName("available_begin_time")]
			public DateTimeOffset? StartTime { get; set; }
			[JsonPropertyName("available_end_time")]
			public DateTimeOffset? FinalTime { get; set; }
			[JsonPropertyName("available_day_after_receive")]
			public int? AvailableDays { get; set; }
			[JsonPropertyName("wait_days_after_receive")]
			public int? WaitDays { get; set; }
		}
		#endregion

		#region 类型转换
		private class CardModeConverter : JsonConverter<CardMode>
		{
			public override CardMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				return reader.GetString() switch
				{
					"SYSTEM_ALLOCATE" => CardMode.Auto,
					"MERCHANT_DEPOSIT" => CardMode.Manual,
					"REAL_TIME" => CardMode.Specified,
					_ => throw new JsonException(),
				};
			}

			public override void Write(Utf8JsonWriter writer, CardMode value, JsonSerializerOptions options)
			{
				switch(value)
				{
					case CardMode.Auto:
						writer.WriteStringValue("SYSTEM_ALLOCATE");
						break;
					case CardMode.Manual:
						writer.WriteStringValue("MERCHANT_DEPOSIT");
						break;
					case CardMode.Specified:
						writer.WriteStringValue("REAL_TIME");
						break;
					default:
						throw new JsonException();
				}
			}
		}

		private class UserFlagsConverter : JsonConverter<UserFlags>
		{
			public override UserFlags Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var flags = UserFlags.None;

				if(reader.TokenType == JsonTokenType.StartArray)
				{
					while(reader.Read())
					{
						if(reader.TokenType == JsonTokenType.EndArray)
							return flags;

						switch(reader.TokenType)
						{
							case JsonTokenType.EndArray:
								return flags;
							case JsonTokenType.String:
								flags |= reader.GetString() switch
								{
									"USER_FORM_FLAG_NAME" => UserFlags.Name,
									"USER_FORM_FLAG_SEX" => UserFlags.Gender,
									"USER_FORM_FLAG_BIRTHDAY" => UserFlags.Birthday,
									"USER_FORM_FLAG_CITY" => UserFlags.City,
									"USER_FORM_FLAG_EMAIL" => UserFlags.Email,
									"USER_FORM_FLAG_MOBILE" => UserFlags.Phone,
									"USER_FORM_FLAG_ADDRESS" => UserFlags.Address,
									_ => UserFlags.None,
								};
								break;
						}
					}
				}

				return flags;
			}

			public override void Write(Utf8JsonWriter writer, UserFlags value, JsonSerializerOptions options)
			{
				writer.WriteStartArray();

				if(value != UserFlags.None)
				{
					if((value & UserFlags.Name) == UserFlags.Name)
						writer.WriteStringValue("USER_FORM_FLAG_NAME");
					if((value & UserFlags.Gender) == UserFlags.Gender)
						writer.WriteStringValue("USER_FORM_FLAG_SEX");
					if((value & UserFlags.Birthday) == UserFlags.Birthday)
						writer.WriteStringValue("USER_FORM_FLAG_BIRTHDAY");
					if((value & UserFlags.City) == UserFlags.City)
						writer.WriteStringValue("USER_FORM_FLAG_CITY");
					if((value & UserFlags.Email) == UserFlags.Email)
						writer.WriteStringValue("USER_FORM_FLAG_EMAIL");
					if((value & UserFlags.Phone) == UserFlags.Phone)
						writer.WriteStringValue("USER_FORM_FLAG_MOBILE");
					if((value & UserFlags.Address) == UserFlags.Address)
						writer.WriteStringValue("USER_FORM_FLAG_ADDRESS");
				}

				writer.WriteEndArray();
			}
		}
		#endregion
	}

	public partial class CardTemplateResponse : CardTemplate
	{
		[JsonPropertyName("card_id")]
		public string TemplateId { get; set; }
		[JsonPropertyName("create_time")]
		public DateTimeOffset? Creation { get; set; }
		[JsonPropertyName("update_time")]
		public DateTimeOffset? Modification { get; set; }
		[JsonPropertyName("remain_quantity")]
		public int RemainQuantity { get; set; }
		[JsonPropertyName("status")]
		public string Status { get; set; }
	}
	#endregion

}
