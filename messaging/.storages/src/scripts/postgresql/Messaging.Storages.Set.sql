INSERT INTO "Messaging_Message" ("Namespace", "Identifier", "Topic", "Identity", "Tags", "Timestamp", "Expiration", "Data")
VALUES (@Namespace, @Identifier, @Topic,
	CASE WHEN @IdentityIsNull THEN NULL ELSE @Identity END,
	CASE WHEN @TagsIsNull THEN NULL ELSE @Tags END,
	@Timestamp,
	CASE WHEN @ExpirationIsNull THEN NULL ELSE @Expiration END,
	CASE WHEN @DataIsNull THEN NULL ELSE @Data END)
ON CONFLICT ("Namespace", "Identifier") DO UPDATE SET
	"Topic"=EXCLUDED."Topic", "Identity"=EXCLUDED."Identity", "Tags"=EXCLUDED."Tags",
	"Timestamp"=EXCLUDED."Timestamp", "Expiration"=EXCLUDED."Expiration", "Data"=EXCLUDED."Data";

