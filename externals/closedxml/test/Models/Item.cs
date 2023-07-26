using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Externals.ClosedXml.Tests.Models;

public class Item
{
	public Item() { }
	public Item(long itemId, string name, string spec = null)
	{
		this.ItemId = itemId;
		this.Name = name;
		this.Spec = spec;
	}

	public long ItemId { get; set; }
	public string ItemNo { get; set; }
	public string Name { get; set; }
	public string Spec { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
}
