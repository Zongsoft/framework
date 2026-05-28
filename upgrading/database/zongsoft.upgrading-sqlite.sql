CREATE TABLE IF NOT EXISTS "Upgrading_Application" (
	"ApplicationId" INTEGER NOT NULL,
	"Name"          TEXT    NOT NULL,
	"Title"         TEXT    NULL,
	"Enabled"       INTEGER NOT NULL DEFAULT 1,
	"Creation"      TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Modification"  TEXT    NULL,
	"Description"   TEXT    NULL,
	PRIMARY KEY ("ApplicationId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Application_Name"
	ON "Upgrading_Application" ("Name");

CREATE TABLE IF NOT EXISTS "Upgrading_ApplicationEdition" (
	"ApplicationId" INTEGER NOT NULL,
	"Name"          TEXT    NOT NULL,
	"Title"         TEXT    NULL,
	"Enabled"       INTEGER NOT NULL DEFAULT 1,
	"Licensed"      INTEGER NOT NULL DEFAULT 0,
	"Creation"      TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Modification"  TEXT    NULL,
	"Description"   TEXT    NULL,
	PRIMARY KEY ("ApplicationId", "Name")
);

CREATE TABLE IF NOT EXISTS "Upgrading_Instance" (
	"InstanceId"   INTEGER NOT NULL,
	"InstanceCode" TEXT    NOT NULL,
	"Name"         TEXT    NULL,
	"Tags"         TEXT    NULL,
	"Profile"      TEXT    NULL,
	"Creation"     TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Modification" TEXT    NULL,
	"Description"  TEXT    NULL,
	PRIMARY KEY ("InstanceId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Instance_Code"
	ON "Upgrading_Instance" ("InstanceCode");

CREATE TABLE IF NOT EXISTS "Upgrading_Release" (
	"ReleaseId"        INTEGER NOT NULL,
	"Name"             TEXT    NOT NULL,
	"Edition"          TEXT    NOT NULL DEFAULT '',
	"Version"          BIGINT  NOT NULL DEFAULT 0,
	"Kind"             INTEGER NOT NULL DEFAULT 0,
	"Mode"             INTEGER NOT NULL DEFAULT 0,
	"Platform"         INTEGER NOT NULL DEFAULT 0,
	"Architecture"     INTEGER NOT NULL DEFAULT 0,
	"Path"             TEXT    NULL,
	"Size"             INTEGER NOT NULL DEFAULT 0,
	"Checksum"         TEXT    NULL,
	"Tags"             TEXT    NULL,
	"Deprecated"       INTEGER NOT NULL DEFAULT 0,
	"Published"        INTEGER NOT NULL DEFAULT 0,
	"Visible"          INTEGER NOT NULL DEFAULT 1,
	"Title"            TEXT    NULL,
	"Summary"          TEXT    NULL,
	"EvaluatorName"    TEXT    NULL,
	"EvaluatorSetting" TEXT    NULL,
	"Creation"         TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Modification"     TEXT    NULL,
	"Description"      TEXT    NULL,
	PRIMARY KEY ("ReleaseId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Release_Key"
	ON "Upgrading_Release" ("Name", "Edition", "Version", "Platform", "Architecture");

CREATE TABLE IF NOT EXISTS "Upgrading_ReleaseProperty" (
	"ReleaseId" INTEGER NOT NULL,
	"Name"      TEXT    NOT NULL,
	"Type"      TEXT    NULL,
	"Value"     TEXT    NULL,
	PRIMARY KEY ("ReleaseId", "Name")
);

CREATE TABLE IF NOT EXISTS "Upgrading_ReleaseExecutor" (
	"ReleaseId" INTEGER NOT NULL,
	"SerialId"  INTEGER NOT NULL,
	"Event"     TEXT    NOT NULL,
	"Command"   TEXT    NOT NULL,
	PRIMARY KEY ("ReleaseId", "SerialId")
);

CREATE TABLE IF NOT EXISTS "Upgrading_ReleasePublishing" (
	"ReleaseId"   INTEGER NOT NULL,
	"InstanceId"  INTEGER NOT NULL,
	"Status"      INTEGER NOT NULL DEFAULT 0,
	"Message"     TEXT    NULL,
	"Timestamp"   TEXT    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Description" TEXT    NULL,
	PRIMARY KEY ("ReleaseId", "InstanceId")
);
