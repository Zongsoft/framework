CREATE DATABASE IF NOT EXISTS zongsoft;

CREATE STABLE IF NOT EXISTS zongsoft.`GatewayHistory`(`Timestamp` timestamp, `Value` double, `Text` varchar(100), `FailureCode` int, `FailureMessage` nchar(500)) TAGS (`GatewayId` int unsigned, `MetricId`  bigint unsigned);
CREATE STABLE IF NOT EXISTS zongsoft.`DeviceHistory`(`Timestamp` timestamp, `Value` double, `Text` varchar(100), `FailureCode` int, `FailureMessage` nchar(500)) TAGS (`DeviceId` bigint unsigned, `MetricId` bigint unsigned);
