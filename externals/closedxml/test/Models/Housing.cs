using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Externals.ClosedXml.Tests.Models;

public class Park
{
	public Park() { }
	public Park(int parkId, string name) : this(parkId, 0, name) { }
	public Park(int parkId, byte term, string name)
	{
		this.ParkId = parkId;
		this.ParkTerm = term;
		this.Name = name;
	}

	public int ParkId { get; set; }
	public byte ParkTerm { get; set; }
	public string Name { get; set; }
	public string AddressDetail { get; set; }
}

public class Building
{
	public Building() { }
	public Building(int buildingId, string name)
	{
		this.BuildingId = buildingId;
		this.Name = name;
	}

	public int BuildingId { get; set; }
	public string BuildingNo { get; set; }
	public string Name { get; set; }
	public int ParkId { get; set; }
	public byte ParkTerm { get; set; }
	public Park Park { get; set; }
}

public class Apartment
{
	public Apartment() { }
	public Apartment(int apartmentId, string doorNo)
	{
		this.ApartmentId = apartmentId;
		this.DoorNo = doorNo;
	}

	public int ApartmentId { get; set; }
	public string ApartmentNo { get; set; }
	public string ApartmentCode { get; set; }
	public string DoorNo { get; set; }
	public int BuildingId { get; set; }
	public Building Building { get; set; }
}

public class ApartmentUsage
{
	public ApartmentUsage() { }
	public ApartmentUsage(Apartment apartment, Asset asset, DateTime date, double quantity)
	{
		this.Apartment = apartment;
		this.ApartmentId = apartment.ApartmentId;
		this.Asset = asset;
		this.AssetId = asset.AssetId;
		this.Date = date;
		this.Quantity = quantity;
	}

	public int ApartmentId { get; set; }
	public Apartment Apartment { get; set; }
	public long AssetId { get; set; }
	public Asset Asset { get; set; }
	public DateTime Date { get; set; }
	public double Quantity { get; set; }
	public float Coefficient { get; set; }
	public string Voucher { get; set; }
	public string VoucherKey { get; set; }
	public string VoucherDescription { get; set; }
	public int CreatorId { get; set; }
	public DateTime CreatedTime { get; set; }
	public User Creator { get; set; }
	public int? ModifierId { get; set; }
	public DateTime? ModifiedTime { get; set; }
	public User Modifier { get; set; }
	public string Remark { get; set; }
}
