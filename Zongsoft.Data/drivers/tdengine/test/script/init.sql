CREATE DATABASE IF NOT EXISTS zongsoft;

/*
 * 注意：TDengine 初始化脚本内的SQL语句不支持换行！
 */

CREATE STABLE IF NOT EXISTS zongsoft.`GatewayHistory`(`Timestamp` timestamp, `Value` double, `Text` varchar(100), `FailureCode` int, `FailureMessage` nchar(500)) TAGS (`GatewayId` int unsigned, `MetricId`  bigint unsigned);
CREATE STABLE IF NOT EXISTS zongsoft.`DeviceHistory`(`Timestamp` timestamp, `Value` double, `Text` varchar(100), `FailureCode` int, `FailureMessage` nchar(500)) TAGS (`DeviceId` bigint unsigned, `MetricId` bigint unsigned);
