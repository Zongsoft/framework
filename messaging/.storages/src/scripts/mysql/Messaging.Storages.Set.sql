INSERT INTO `Messaging_Message` (`Namespace`, `Identifier`, `Topic`, `Identity`, `Tags`, `Timestamp`, `Expiration`, `Data`)
VALUES (@Namespace, @Identifier, @Topic,
	CASE WHEN @IdentityIsNull=1 THEN NULL ELSE @Identity END,
	CASE WHEN @TagsIsNull=1 THEN NULL ELSE @Tags END,
	@Timestamp,
	CASE WHEN @ExpirationIsNull=1 THEN NULL ELSE @Expiration END,
	CASE WHEN @DataIsNull=1 THEN NULL ELSE @Data END)
ON DUPLICATE KEY UPDATE
	`Topic`=VALUES(`Topic`), `Identity`=VALUES(`Identity`), `Tags`=VALUES(`Tags`),
	`Timestamp`=VALUES(`Timestamp`), `Expiration`=VALUES(`Expiration`), `Data`=VALUES(`Data`);

