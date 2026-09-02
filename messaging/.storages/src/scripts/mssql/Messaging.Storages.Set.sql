UPDATE [Messaging_Message] SET
	[Topic]=@Topic,
	[Identity]=CASE WHEN @IdentityIsNull=1 THEN NULL ELSE @Identity END,
	[Tags]=CASE WHEN @TagsIsNull=1 THEN NULL ELSE @Tags END,
	[Timestamp]=@Timestamp,
	[Expiration]=CASE WHEN @ExpirationIsNull=1 THEN NULL ELSE @Expiration END,
	[Data]=CASE WHEN @DataIsNull=1 THEN NULL ELSE @Data END
WHERE [Namespace]=@Namespace AND [Identifier]=@Identifier;
IF @@ROWCOUNT=0
BEGIN
	BEGIN TRY
		INSERT INTO [Messaging_Message] ([Namespace], [Identifier], [Topic], [Identity], [Tags], [Timestamp], [Expiration], [Data])
		VALUES (@Namespace, @Identifier, @Topic,
			CASE WHEN @IdentityIsNull=1 THEN NULL ELSE @Identity END,
			CASE WHEN @TagsIsNull=1 THEN NULL ELSE @Tags END,
			@Timestamp,
			CASE WHEN @ExpirationIsNull=1 THEN NULL ELSE @Expiration END,
			CASE WHEN @DataIsNull=1 THEN NULL ELSE @Data END);
	END TRY
	BEGIN CATCH
		IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
		UPDATE [Messaging_Message] SET
			[Topic]=@Topic,
			[Identity]=CASE WHEN @IdentityIsNull=1 THEN NULL ELSE @Identity END,
			[Tags]=CASE WHEN @TagsIsNull=1 THEN NULL ELSE @Tags END,
			[Timestamp]=@Timestamp,
			[Expiration]=CASE WHEN @ExpirationIsNull=1 THEN NULL ELSE @Expiration END,
			[Data]=CASE WHEN @DataIsNull=1 THEN NULL ELSE @Data END
		WHERE [Namespace]=@Namespace AND [Identifier]=@Identifier;
	END CATCH
END

