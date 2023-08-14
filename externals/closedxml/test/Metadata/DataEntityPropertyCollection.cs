using System;
using System.Data;

namespace Zongsoft.Data.Metadata;

internal class DataEntityPropertyCollection : NamedCollectionBase<IDataEntityProperty>, IDataEntityPropertyCollection
{
    public DataEntityPropertyCollection(IDataEntity entity) => this.Entity = entity;
    public IDataEntity Entity { get; }
    protected override string GetKeyForItem(IDataEntityProperty property) => property.Name;
	protected override void AddItem(IDataEntityProperty property)
	{
		if(property == null)
			throw new ArgumentNullException(nameof(property));

		((DataEntityProperty)property).Entity = this.Entity;
		base.AddItem(property);
	}
	protected override void SetItem(string name, IDataEntityProperty value)
	{
        if(value == null)
			throw new ArgumentNullException(nameof(value));

		((DataEntityProperty)value).Entity = this.Entity;
        base.SetItem(name, value);
	}
}

internal static class DataEntityPropertyCollectionExtension
{
	public static DataEntitySimplexProperty Simplex(this IDataEntityPropertyCollection properties, string name, DbType type, bool nullable)
	{
		var property = new DataEntitySimplexProperty(name, type, nullable);
		properties.Add(property);
		return property;
	}
	public static DataEntitySimplexProperty Simplex(this IDataEntityPropertyCollection properties, string name, DbType type, int length, bool nullable)
	{
		var property = new DataEntitySimplexProperty(name, type, length, nullable);
		properties.Add(property);
		return property;
	}
	public static DataEntitySimplexProperty Simplex(this IDataEntityPropertyCollection properties, string name, DbType type, byte precision, byte scale, bool nullable)
	{
		var property = new DataEntitySimplexProperty(name, type, precision, scale, nullable);
		properties.Add(property);
		return property;
	}

	public static DataEntityComplexProperty Complex(this IDataEntityPropertyCollection properties, string name, string port, params DataAssociationLink[] links)
	{
		var property = new DataEntityComplexProperty(name, port, links);
		properties.Add(property);
		return property;
	}
	public static DataEntityComplexProperty Complex(this IDataEntityPropertyCollection properties, string name, string port, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
	{
		var property = new DataEntityComplexProperty(name, port, multiplicity, links);
		properties.Add(property);
		return property;
	}
}
