using System;
using System.Collections.Generic;

namespace Zongsoft.Data.TDengine.Tests.Models;

public struct GatewayHistory
{
	#region 构造函数
	public GatewayHistory(uint gatewayId, ulong metricId, double value, string text, DateTime timestamp)
	{
		this.GatewayId = gatewayId;
		this.MetricId = metricId;
		this.Value = value;
		this.Text = value == 0 ? text : null;
		this.Timestamp = timestamp > DateTime.MinValue ? timestamp : DateTime.Now;
	}

	public GatewayHistory(uint gatewayId, ulong metricId, int failureCode, string failureMessage, DateTime timestamp)
	{
		this.GatewayId = gatewayId;
		this.MetricId = metricId;
		this.Value = 0;
		this.Text = null;
		this.FailureCode = failureCode;
		this.FailureMessage = failureMessage;
		this.Timestamp = timestamp > DateTime.MinValue ? timestamp : DateTime.Now;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置网关编号。</summary>
	public uint GatewayId { get; set; }

	/// <summary>获取或设置指标编号。</summary>
	public ulong MetricId { get; set; }

	/// <summary>获取或设置指标值。</summary>
	public double Value { get; set; }

	/// <summary>获取或设置字符串指标值。</summary>
	public string Text { get; set; }

	/// <summary>获取或设置故障代号。</summary>
	public int FailureCode { get; set; }

	/// <summary>获取或设置故障信息。</summary>
	public string FailureMessage { get; set; }

	/// <summary>获取或设置采集时间。</summary>
	public DateTime Timestamp { get; set; }
	#endregion
}
