using System;
using System.Data;

using Zongsoft.Data;
using Zongsoft.Data.Metadata;
using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

internal class Templates
{
	public static readonly UserModel User = new();
	public static readonly AssetUsageModel AssetUsage = new();
	public static readonly ApartmentUsageModel ApartmentUsage = new();

	public class UserModel
	{
		private readonly IDataEntity _entity;

		internal UserModel()
		{
			this.Data = new User[]
			{
				new(101, "Popeye", "Popeye Zhong", Gender.Male)
				{
					Phone ="18912345678",
					Email="zongsoft@gmail.com"
				},
				new(102, "Grape", "Grape Liu", Gender.Female, new DateTime(1983, 1, 23)),
				new(103, "Elsa", "Elsa Zhong", Gender.Female, new DateTime(2015, 3, 23)),
				new(104, "Lucy", "Lucy Zhong", Gender.Female, new DateTime(2019, 5, 13)),
				new(105, "Jack", "Jack Ma"),
			};

			_entity = new DataEntity(nameof(Models.User));
			_entity.Properties.Simplex(nameof(Models.User.UserId), DbType.Int32, false).IsPrimaryKey = true;
			_entity.Properties.Simplex(nameof(Models.User.Name), DbType.String, 50, false);
			_entity.Properties.Simplex(nameof(Models.User.Nickname), DbType.String, 100, false);
			_entity.Properties.Simplex(nameof(Models.User.Email), DbType.AnsiString, 100, false);
			_entity.Properties.Simplex(nameof(Models.User.Phone), DbType.AnsiString, 100, false);
			_entity.Properties.Simplex(nameof(Models.User.Gender), DbType.Byte, false);
			_entity.Properties.Simplex(nameof(Models.User.Birthday), DbType.Date, false);
			_entity.Properties.Simplex(nameof(Models.User.Description), DbType.String, 500, true);

			Descriptor = _entity.GetDescriptor<Models.User>();
		}

		public User[] Data { get; }
		public ModelDescriptor Descriptor { get; }
	}

	public class AssetUsageModel
	{
		public AssetUsageModel()
		{
			var entity = new DataEntity(nameof(Models.AssetUsage));
			entity.Properties.Simplex(nameof(Models.AssetUsage.AssetId), DbType.Int64, false).IsPrimaryKey = true;
			entity.Properties.Simplex(nameof(Models.AssetUsage.Date), DbType.Date, false).IsPrimaryKey = true;
			entity.Properties.Simplex(nameof(Models.AssetUsage.Quantity), DbType.Double, 12, 4, false);
			entity.Properties.Simplex(nameof(Models.AssetUsage.Coefficient), DbType.Single, 7, 3, false);
			entity.Properties.Simplex(nameof(Models.AssetUsage.Voucher), DbType.AnsiString, 50, true);
			entity.Properties.Simplex(nameof(Models.AssetUsage.VoucherKey), DbType.AnsiString, 100, true);
			entity.Properties.Simplex(nameof(Models.AssetUsage.VoucherDescription), DbType.String, 200, true);
			entity.Properties.Simplex(nameof(Models.AssetUsage.CreatorId), DbType.Int32, false);
			entity.Properties.Simplex(nameof(Models.AssetUsage.CreatedTime), DbType.DateTime, false);
			entity.Properties.Simplex(nameof(Models.AssetUsage.ModifierId), DbType.Int32, true);
			entity.Properties.Simplex(nameof(Models.AssetUsage.ModifiedTime), DbType.DateTime, true);

			this.Descriptor = entity.GetDescriptor<AssetUsage>();
		}

		public ModelDescriptor Descriptor { get; }
	}

	public class ApartmentUsageModel
	{
		private SpreadsheetTemplateProvider _provider;

		public ApartmentUsageModel()
		{
			this.Park = Parks[0];
			this.Usages = new[]
			{
				new ApartmentUsage(101, 10001)
				{
					Apartment = Apartments[0],
					Asset = Assets[0],
					AssetId = Assets[0].AssetId,
					Latest = new AssetUsage(Assets[0].AssetId, DateTime.Today, 100),
				},
				new ApartmentUsage(101, 10002)
				{
					Apartment = Apartments[0],
					Asset = Assets[1],
					AssetId = Assets[1].AssetId,
					Latest = new AssetUsage(Assets[1].AssetId, DateTime.Today, 200),
				},
				new ApartmentUsage(102, 10002)
				{
					Apartment = Apartments[1],
					Asset = Assets[0],
					AssetId = Assets[0].AssetId,
				},
				new ApartmentUsage(102, 10002)
				{
					Apartment = Apartments[1],
					Asset = Assets[1],
					AssetId = Assets[1].AssetId,
					Latest = new AssetUsage(Assets[1].AssetId, DateTime.Today, 300),
				}
			};
		}

		public Park Park { get; }
		public ApartmentUsage[] Usages { get; }

		public IDataTemplate Template
		{
			get
			{
				_provider ??= new(Path.Combine(Environment.CurrentDirectory, "../../../templates/"));
				return _provider.GetTemplate("apartment.usages");
			}
		}

		private static readonly Item[] Items = new[]
		{
			new Item(1101, "水费"),
			new Item(1201, "电费"),
		};

		private static readonly Asset[] Assets = new[]
		{
			new Asset(10001, "一号设施", "水表")
			{
				AssetNo = "A10001",
				Item = Items[0],
				ItemId = Items[0].ItemId,
			},
			new Asset(10002, "二号设施", "电表")
			{
				AssetNo = "A10002",
				Item = Items[1],
				ItemId = Items[1].ItemId,
			},
		};

		private static readonly Park[] Parks = new[]
		{
			new Park(1, "武汉万科红郡")
			{
				AddressDetail = "湖北省武汉市东湖高新开发区·大学园路1号",
			},
		};

		private static readonly Building[] Buildings = new[]
		{
			new Building(1, "一号大楼")
			{
				Park = Parks[0],
				ParkId = Parks[0].ParkId,
				ParkTerm = Parks[0].ParkTerm,
			},
			new Building(2, "二号大楼")
			{
				Park = Parks[0],
				ParkId = Parks[0].ParkId,
				ParkTerm = Parks[0].ParkTerm,
			},
			new Building(3, "三号大楼")
			{
				Park = Parks[0],
				ParkId = Parks[0].ParkId,
				ParkTerm = Parks[0].ParkTerm,
			},
		};

		private static readonly Apartment[] Apartments = new[]
		{
			new Apartment(101, "1-101")
			{
				Building = Buildings[0],
				BuildingId = Buildings[0].BuildingId,
			},
			new Apartment(102, "1-102")
			{
				Building = Buildings[0],
				BuildingId = Buildings[0].BuildingId,
			},
		};
	}
}
