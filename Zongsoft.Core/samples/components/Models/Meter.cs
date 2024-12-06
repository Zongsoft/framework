using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Components.Samples.Models;

public partial struct Meter : IEquatable<Meter>
{
	#region 构造函数
	public Meter(string key, string code, params Metric[] metrics)
	{
		this.Key = key?.ToUpperInvariant();
		this.Code = code;
		this.Timestamp = DateTime.Now;
		this.Metrics = new MetricCollection(metrics);
	}
	public Meter(string key, string code, DateTime timestamp, params Metric[] metrics)
	{
		this.Key = key?.ToUpperInvariant();
		this.Code = code;
		this.Timestamp = timestamp;
		this.Metrics = new MetricCollection(metrics);
	}
	public Meter(string key, string code, IEnumerable<Metric> metrics)
	{
		this.Key = key?.ToUpperInvariant();
		this.Code = code;
		this.Timestamp = DateTime.Now;
		this.Metrics = new MetricCollection(metrics);
	}
	public Meter(string key, string code, DateTime timestamp, IEnumerable<Metric> metrics)
	{
		this.Key = key?.ToUpperInvariant();
		this.Code = code;
		this.Timestamp = timestamp;
		this.Metrics = new MetricCollection(metrics);
	}
	#endregion

	#region 公共字段
	public string Key;
	public string Code;
	public DateTime Timestamp;
	public MetricCollection Metrics;
	#endregion

	#region 公共属性
	[System.Text.Json.Serialization.JsonIgnore]
	[Zongsoft.Serialization.SerializationMember(Ignored = true)]
	public bool IsEmpty => this.Metrics == null || this.Metrics.Count == 0;
	#endregion

	#region 重写方法
	public bool Equals(Meter other) => string.Equals(this.Key, other.Key);
	public override bool Equals(object obj) => obj is Meter other && this.Equals(other);
	public override int GetHashCode() => string.IsNullOrEmpty(this.Key) ? 0 : this.Key.GetHashCode();
	public override string ToString() => $"{this.Key}({this.Metrics.Count})";
	#endregion

	#region 重写符号
	public static bool operator ==(Meter left, Meter right) => left.Equals(right);
	public static bool operator !=(Meter left, Meter right) => !(left == right);
	#endregion
}

public class MeterCollection : KeyedCollection<string, Meter>
{
	public MeterCollection() : base(StringComparer.OrdinalIgnoreCase) { }
	protected override string GetKeyForItem(Meter meter) => meter.Key;
}
