CREATE TABLE IF NOT EXISTS `Messaging_Message`
(
	`Namespace`  VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
	`Identifier` VARCHAR(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
	`Topic`      VARCHAR(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL DEFAULT '',
	`Identity`   TEXT         CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL,
	`Tags`       TEXT         CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL,
	`Timestamp`  DATETIME(6)  NOT NULL,
	`Expiration` DATETIME(6)  NULL,
	`Data`       LONGBLOB     NULL,
	CONSTRAINT `PK_Messaging_Message` PRIMARY KEY (`Namespace`, `Identifier`),
	INDEX `IX_Messaging_Message_Namespace_Topic_Expiration` (`Namespace`, `Topic`, `Expiration`),
	INDEX `IX_Messaging_Message_Namespace_Expiration` (`Namespace`, `Expiration`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin;
