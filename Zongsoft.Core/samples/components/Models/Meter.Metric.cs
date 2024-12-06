using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Components.Samples.Models;

partial struct Meter
{
	public struct Metric(string key, string code, object value) : IEquatable<Metric>
	{
		#region 公共字段
		public string Key = key;
		public string Code = code;

		[System.Text.Json.Serialization.JsonConverter(typeof(Zongsoft.Serialization.Json.ObjectConverter))]
		public object Value = value;
		#endregion

		#region 公共属性
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public bool HasValue => this.Value != null;
		#endregion

		#region 公共方法
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public bool IsFailure => this.Value is Failure;
		public bool HasFailure(out Failure failure)
		{
			failure = this.Value as Failure;
			return failure != null;
		}
		#endregion

		#region 重写方法
		public bool Equals(Metric other) => string.Equals(this.Key, other.Key);
		public override bool Equals(object obj) => obj is Metric other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Key);
		public override string ToString() => $"{this.Key}={this.Value}";
		#endregion

		#region 重写符号
		public static bool operator ==(Metric left, Metric right) => left.Equals(right);
		public static bool operator !=(Metric left, Metric right) => !(left == right);
		#endregion
	}

	public class MetricCollection : KeyedCollection<string, Metric>
	{
		#region 构造函数
		public MetricCollection() { }
		public MetricCollection(IEnumerable<Metric> metrics) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(metrics != null)
			{
				foreach(var metric in metrics)
					this.Items.Add(metric);
			}
		}
		#endregion

		#region 公共方法
		public Metric Add(string key, string code, object value)
		{
			var metric = new Metric(key, code, value);
			this.Items.Add(metric);
			return metric;
		}

		public bool TryAdd(string key, string code, object value, out Metric result)
		{
			if(Contains(key))
			{
				result = default;
				return false;
			}

			result = new Metric(key, code, value);
			this.Items.Add(result);
			return true;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Metric metric) => metric.Key;
		#endregion
	}
}