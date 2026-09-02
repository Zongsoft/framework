IF OBJECT_ID(N'[dbo].[Messaging_Message]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[Messaging_Message]
	(
		[Namespace]  NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
		[Identifier] NVARCHAR(512) COLLATE Latin1_General_100_BIN2 NOT NULL,
		[Topic]      NVARCHAR(512) COLLATE Latin1_General_100_BIN2 NOT NULL CONSTRAINT [DF_Messaging_Message_Topic] DEFAULT N'',
		[Identity]   NVARCHAR(MAX) NULL,
		[Tags]       NVARCHAR(MAX) NULL,
		[Timestamp]  DATETIME2(6)  NOT NULL,
		[Expiration] DATETIME2(6)  NULL,
		[Data]       VARBINARY(MAX) NULL,
		CONSTRAINT [PK_Messaging_Message] PRIMARY KEY NONCLUSTERED ([Namespace], [Identifier])
	);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[dbo].[Messaging_Message]') AND name=N'IX_Messaging_Message_Namespace_Expiration')
	CREATE CLUSTERED INDEX [IX_Messaging_Message_Namespace_Expiration]
		ON [dbo].[Messaging_Message] ([Namespace], [Expiration]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'[dbo].[Messaging_Message]') AND name=N'IX_Messaging_Message_Namespace_Topic_Expiration')
	CREATE NONCLUSTERED INDEX [IX_Messaging_Message_Namespace_Topic_Expiration]
		ON [dbo].[Messaging_Message] ([Namespace], [Topic], [Expiration]);
