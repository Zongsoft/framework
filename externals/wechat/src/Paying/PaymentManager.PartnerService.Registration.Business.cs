using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class PartnerService
	{
		public partial class Registration
		{
			#region 嵌套类型
			public sealed class Business
			{
				[JsonPropertyName("merchant_shortname")]
				public string MerchantShortname { get; set; }

				[JsonPropertyName("service_phone")]
				public string ServicePhone { get; set; }

				[JsonPropertyName("sales_info")]
				public Sales SalesInfo { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.MerchantShortname))
						throw new ArgumentException(null, nameof(this.MerchantShortname));

					if(string.IsNullOrWhiteSpace(this.ServicePhone))
						throw new ArgumentException(null, nameof(this.ServicePhone));

					if(this.SalesInfo == null)
						throw new ArgumentException(null, nameof(this.SalesInfo));

					this.SalesInfo?.Validate();
				}
			}

			public sealed class Sales
			{
				[JsonPropertyName("sales_scenes_type")]
				public string[] SalesScenesType { get; set; }

				[JsonPropertyName("biz_store_info")]
				public BizStore BizStoreInfo { get; set; }

				[JsonPropertyName("mp_info")]
				public Mp MpInfo { get; set; }

				[JsonPropertyName("mini_program_info")]
				public MiniProgram MiniProgramInfo { get; set; }

				[JsonPropertyName("app_info")]
				public App AppInfo { get; set; }

				[JsonPropertyName("web_info")]
				public Web WebInfo { get; set; }

				[JsonPropertyName("wework_info")]
				public Wework WeworkInfo { get; set; }

				internal void Validate()
				{
					if(this.SalesScenesType == null || this.SalesScenesType.Length == 0)
						throw new ArgumentException(null, nameof(this.SalesScenesType));

					if(this.SalesScenesType != null)
					{
						foreach(var item in this.SalesScenesType)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.SalesScenesType));
						}
					}

					this.BizStoreInfo?.Validate();

					this.MpInfo?.Validate();

					this.MiniProgramInfo?.Validate();

					this.AppInfo?.Validate();

					this.WebInfo?.Validate();

					this.WeworkInfo?.Validate();
				}
			}

			public sealed class BizStore
			{
				[JsonPropertyName("biz_store_name")]
				public string BizStoreName { get; set; }

				[JsonPropertyName("biz_address_code")]
				public string BizAddressCode { get; set; }

				[JsonPropertyName("biz_store_address")]
				public string BizStoreAddress { get; set; }

				[JsonPropertyName("store_entrance_pic")]
				public string[] StoreEntrancePic { get; set; }

				[JsonPropertyName("indoor_pic")]
				public string[] IndoorPic { get; set; }

				[JsonPropertyName("biz_sub_appid")]
				public string BizSubAppid { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.BizStoreName))
						throw new ArgumentException(null, nameof(this.BizStoreName));

					if(string.IsNullOrWhiteSpace(this.BizAddressCode))
						throw new ArgumentException(null, nameof(this.BizAddressCode));

					if(string.IsNullOrWhiteSpace(this.BizStoreAddress))
						throw new ArgumentException(null, nameof(this.BizStoreAddress));

					if(this.StoreEntrancePic == null || this.StoreEntrancePic.Length == 0)
						throw new ArgumentException(null, nameof(this.StoreEntrancePic));

					if(this.StoreEntrancePic != null)
					{
						foreach(var item in this.StoreEntrancePic)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.StoreEntrancePic));
						}
					}

					if(this.IndoorPic == null || this.IndoorPic.Length == 0)
						throw new ArgumentException(null, nameof(this.IndoorPic));

					if(this.IndoorPic != null)
					{
						foreach(var item in this.IndoorPic)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.IndoorPic));
						}
					}
				}
			}

			public sealed class Mp
			{
				[JsonPropertyName("mp_appid")]
				public string MpAppid { get; set; }

				[JsonPropertyName("mp_sub_appid")]
				public string MpSubAppid { get; set; }

				[JsonPropertyName("mp_pics")]
				public string[] MpPics { get; set; }

				internal void Validate()
				{
					if(this.MpPics == null || this.MpPics.Length == 0)
						throw new ArgumentException(null, nameof(this.MpPics));

					if(this.MpPics != null)
					{
						foreach(var item in this.MpPics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.MpPics));
						}
					}
				}
			}

			public sealed class MiniProgram
			{
				[JsonPropertyName("mini_program_appid")]
				public string MiniProgramAppid { get; set; }

				[JsonPropertyName("mini_program_sub_appid")]
				public string MiniProgramSubAppid { get; set; }

				[JsonPropertyName("mini_program_pics")]
				public string[] MiniProgramPics { get; set; }

				internal void Validate()
				{
					if(this.MiniProgramPics != null)
					{
						foreach(var item in this.MiniProgramPics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.MiniProgramPics));
						}
					}
				}
			}

			public sealed class App
			{
				[JsonPropertyName("app_appid")]
				public string AppAppid { get; set; }

				[JsonPropertyName("app_sub_appid")]
				public string AppSubAppid { get; set; }

				[JsonPropertyName("app_pics")]
				public string[] AppPics { get; set; }

				internal void Validate()
				{
					if(this.AppPics == null || this.AppPics.Length == 0)
						throw new ArgumentException(null, nameof(this.AppPics));

					if(this.AppPics != null)
					{
						foreach(var item in this.AppPics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.AppPics));
						}
					}
				}
			}

			public sealed class Web
			{
				[JsonPropertyName("domain")]
				public string Domain { get; set; }

				[JsonPropertyName("web_authorisation")]
				public string WebAuthorisation { get; set; }

				[JsonPropertyName("web_appid")]
				public string WebAppid { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.Domain))
						throw new ArgumentException(null, nameof(this.Domain));
				}
			}

			public sealed class Wework
			{
				[JsonPropertyName("sub_corp_id")]
				public string SubCorpId { get; set; }

				[JsonPropertyName("wework_pics")]
				public string[] WeworkPics { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.SubCorpId))
						throw new ArgumentException(null, nameof(this.SubCorpId));

					if(this.WeworkPics == null || this.WeworkPics.Length == 0)
						throw new ArgumentException(null, nameof(this.WeworkPics));

					if(this.WeworkPics != null)
					{
						foreach(var item in this.WeworkPics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.WeworkPics));
						}
					}
				}
			}
			#endregion
		}
	}
}
