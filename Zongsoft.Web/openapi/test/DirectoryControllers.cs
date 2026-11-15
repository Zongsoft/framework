using Microsoft.AspNetCore.Mvc;

using Zongsoft.Services;

[assembly: ApplicationModule("Catalog")]

namespace Zongsoft.Web.OpenApi.Tests;

[Route("catalog")]
public class CatalogController : ControllerBase
{
	[HttpGet("inventory")]
	[HttpGet("inventory-alias")]
	public string ListInventory() => "inventory";

	public static class Inventory
	{
		public static class Warehouses
		{
			[Route("catalog/warehouses")]
			public class ItemsController : ControllerBase
			{
				[HttpGet("{id}")]
				public string Find(string id) => id;
			}
		}
	}

	public static class Returns
	{
		public static class Warehouses
		{
			[Route("catalog/returns/warehouses")]
			public class ReturnItemsController : ControllerBase
			{
				[HttpGet("~/returns/items")]
				public string ListReturnItems() => "returns";
			}
		}
	}
}

public class BrowseController : ControllerBase
{
	[HttpGet]
	public string List() => "catalog";
}
