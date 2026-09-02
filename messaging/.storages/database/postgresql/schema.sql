CREATE TABLE IF NOT EXISTS "Messaging_Message"
(
	"Namespace"  VARCHAR(128) COLLATE "C" NOT NULL,
	"Identifier" VARCHAR(512) COLLATE "C" NOT NULL,
	"Topic"      VARCHAR(512) COLLATE "C" NOT NULL DEFAULT '',
	"Identity"   TEXT         NULL,
	"Tags"       TEXT         NULL,
	"Timestamp"  TIMESTAMP WITH TIME ZONE NOT NULL,
	"Expiration" TIMESTAMP WITH TIME ZONE NULL,
	"Data"       BYTEA        NULL,
	CONSTRAINT "PK_Messaging_Message" PRIMARY KEY ("Namespace", "Identifier")
);

CREATE INDEX IF NOT EXISTS "IX_Messaging_Message_Namespace_Topic_Expiration"
	ON "Messaging_Message" ("Namespace", "Topic", "Expiration");

CREATE INDEX IF NOT EXISTS "IX_Messaging_Message_Namespace_Expiration"
	ON "Messaging_Message" ("Namespace", "Expiration");
