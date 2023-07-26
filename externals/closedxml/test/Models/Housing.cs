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
	public ApartmentUsage(int apartmentId, long assetId)
	{
		this.ApartmentId = apartmentId;
		this.AssetId = assetId;
	}

	public int ApartmentId { get; set; }
	public Apartment Apartment { get; set; }
	public long AssetId { get; set; }
	public Asset Asset { get; set; }
	public AssetUsage Latest { get; set; }
}
