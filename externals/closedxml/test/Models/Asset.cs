using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Externals.ClosedXml.Tests.Models;

public class Asset
{
	public Asset() { }
	public Asset(long assetId, string name, string spec)
	{
		this.AssetId = assetId;
		this.Name = name;
		this.Spec = spec;
	}

	public long AssetId { get; set; }
	public string AssetNo { get; set; }
	public string AssetCode { get; set; }
	public string Name { get; set; }
	public string Spec { get; set; }
	public long ItemId { get; set; }
	public Item Item { get; set; }
}

public class AssetUsage
{
	public AssetUsage() { }
	public AssetUsage(long assetId, DateTime date, double quantity, string voucher = null)
	{
		this.AssetId = assetId;
		this.Date = date;
		this.Quantity = quantity;

		if(!string.IsNullOrEmpty(voucher))
		{
			if(voucher == "_" || voucher == "-")
			{
				this.Voucher = "_";
				this.VoucherKey = "404";
			}
			else
			{
				var index = voucher.IndexOf(':');
				if(index > 0 && index < voucher.Length - 1)
				{
					this.Voucher = voucher[..index];
					this.VoucherKey = voucher[(index + 1)..];
				}
			}
		}
	}

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