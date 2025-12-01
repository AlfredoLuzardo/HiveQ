-- Add profile picture columns to Users table
ALTER TABLE Users ADD COLUMN ProfilePicture BLOB;
ALTER TABLE Users ADD COLUMN ProfilePictureContentType TEXT;

-- Mark the migration as applied
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251117034405_InitialCreate', '9.0.0');

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251201124508_AddProfilePictureToUser', '9.0.0');
