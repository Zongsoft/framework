SELECT `Identifier`, `Topic`, `Identity`, `Tags`, `Timestamp`, `Data` FROM `Messaging_Message`
WHERE `Namespace`=@Namespace AND `Topic`=@Topic AND (`Expiration` IS NULL OR `Expiration`>@Timestamp);

