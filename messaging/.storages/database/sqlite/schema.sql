CREATE TABLE IF NOT EXISTS "Messaging_Message"
(
	"Namespace"  TEXT     COLLATE BINARY NOT NULL CHECK (length("Namespace") <= 128),
	"Identifier" TEXT     COLLATE BINARY NOT NULL CHECK (length("Identifier") <= 512),
	"Topic"      TEXT     COLLATE BINARY NOT NULL DEFAULT '' CHECK (length("Topic") <= 512),
	"Identity"   TEXT     NULL,
	"Tags"       TEXT     NULL,
	"Timestamp"  DATETIME NOT NULL,
	"Expiration" DATETIME NULL,
	"Data"       BLOB     NULL,
	CONSTRAINT "PK_Messaging_Message" PRIMARY KEY ("Namespace", "Identifier")
) WITHOUT ROWID;

CREATE INDEX IF NOT EXISTS "IX_Messaging_Message_Namespace_Topic_Expiration"
	ON "Messaging_Message" ("Namespace", "Topic", "Expiration");

CREATE INDEX IF NOT EXISTS "IX_Messaging_Message_Namespace_Expiration"
	ON "Messaging_Message" ("Namespace", "Expiration");
