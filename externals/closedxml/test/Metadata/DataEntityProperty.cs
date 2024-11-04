using System;
using System.Data;

namespace Zongsoft.Data.Metadata;

internal abstract class DataEntityProperty : IDataEntityProperty
{
	public IDataEntity Entity { get; internal set; }
	public string Name { get; set; }
	public string Hint { get; set; }
	public bool Immutable { get; set; }
	public bool IsPrimaryKey { get; set; }
	public abstract bool IsSimplex { get; }
	public abstract bool IsComplex { get; }
	public bool Equals(IDataEntityProperty other) => other != null && this.Entity == other.Entity && string.Equals(this.Name, other.Name);
}

internal class DataEntitySimplexProperty : DataEntityProperty, IDataEntitySimplexProperty
{
    public DataEntitySimplexProperty(string name, DbType type, bool nullable)
    {
        this.Name = name;
		this.Type = type;
		this.Nullable = nullable;
    }

    public DataEntitySimplexProperty(string name, DbType type, int length, bool nullable)
    {
		this.Name = name;
		this.Type = type;
		this.Length = length;
		this.Nullable = nullable;
    }

	public DataEntitySimplexProperty(string name, DbType type, byte precision, byte scale, bool nullable)
	{
		this.Name = name;
		this.Type = type;
		this.Precision = precision;
		this.Scale = scale;
		this.Nullable = nullable;
	}

    public string Alias { get; set; }
	public DbType Type { get; set; }
	public int Length { get; set; }
	public byte Precision { get; set; }
	public byte Scale { get; set; }
	public object DefaultValue { get; set; }
	public bool Nullable { get; set; }
	public bool Sortable { get; set; }
	public IDataEntityPropertySequence Sequence => null;

	public override bool IsSimplex => true;
	public override bool IsComplex => false;
}

internal class DataEntityComplexProperty : DataEntityProperty, IDataEntityComplexProperty
{
	public DataEntityComplexProperty(string name, string port, params DataAssociationLink[] links)
	{
		this.Name = name;
		this.Port = port;
		this.Links = links;
	}

	public DataEntityComplexProperty(string name, string port, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
    {
		this.Name = name;
		this.Port = port;
		this.Multiplicity = multiplicity;
		this.Links = links;
    }

    public DataEntityComplexPropertyBehaviors Behaviors { get; set; }
	public IDataEntity Foreign { get; set; }
	public IDataEntityProperty ForeignProperty { get; set; }
	public DataAssociationMultiplicity Multiplicity { get; set; }
	public string Port { get; set; }
	public DataAssociationLink[] Links { get; set; }
	public DataAssociationConstraint[] Constraints { get; set; }

	public override bool IsSimplex => false;
	public override bool IsComplex => true;
}
