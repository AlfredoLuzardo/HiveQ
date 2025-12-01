CREATE TABLE "Users" (
    "UserId" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "PhoneNumber" TEXT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "ProfilePicture" BLOB NULL,
    "ProfilePictureContentType" TEXT NULL
);


CREATE TABLE "Queues" (
    "QueueId" INTEGER NOT NULL CONSTRAINT "PK_Queues" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "QueueName" TEXT NOT NULL,
    "Description" TEXT NULL,
    "QRCodeData" TEXT NOT NULL,
    "QRCodeImageUrl" TEXT NULL,
    "Status" TEXT NOT NULL,
    "MaxCapacity" INTEGER NOT NULL,
    "EstimatedWaitTimePerPerson" INTEGER NOT NULL,
    "CurrentQueueSize" INTEGER NOT NULL,
    "TotalServedToday" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    CONSTRAINT "FK_Queues_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);


CREATE TABLE "QueueEntries" (
    "QueueEntryId" INTEGER NOT NULL CONSTRAINT "PK_QueueEntries" PRIMARY KEY AUTOINCREMENT,
    "QueueId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "PositionNumber" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "JoinedAt" TEXT NOT NULL,
    "NotifiedAt" TEXT NULL,
    "ServedAt" TEXT NULL,
    "EstimatedWaitTime" INTEGER NOT NULL,
    "NotificationPreference" TEXT NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_QueueEntries_Queues_QueueId" FOREIGN KEY ("QueueId") REFERENCES "Queues" ("QueueId") ON DELETE CASCADE,
    CONSTRAINT "FK_QueueEntries_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
);


CREATE TABLE "Notifications" (
    "NotificationId" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
    "QueueEntryId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "Channel" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "SentAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Notifications_QueueEntries_QueueEntryId" FOREIGN KEY ("QueueEntryId") REFERENCES "QueueEntries" ("QueueEntryId") ON DELETE CASCADE,
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
);


CREATE TABLE "QueueHistories" (
    "HistoryId" INTEGER NOT NULL CONSTRAINT "PK_QueueHistories" PRIMARY KEY AUTOINCREMENT,
    "QueueId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "QueueEntryId" INTEGER NULL,
    "JoinedAt" TEXT NOT NULL,
    "ServedAt" TEXT NULL,
    "WaitTime" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "Date" TEXT NOT NULL,
    CONSTRAINT "FK_QueueHistories_QueueEntries_QueueEntryId" FOREIGN KEY ("QueueEntryId") REFERENCES "QueueEntries" ("QueueEntryId") ON DELETE SET NULL,
    CONSTRAINT "FK_QueueHistories_Queues_QueueId" FOREIGN KEY ("QueueId") REFERENCES "Queues" ("QueueId") ON DELETE CASCADE,
    CONSTRAINT "FK_QueueHistories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
);


INSERT INTO "Users" ("UserId", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "PhoneNumber", "ProfilePicture", "ProfilePictureContentType")
VALUES (1, '2024-01-01 00:00:00', 'owner@coffeeshop.com', 'Sarah', 1, 'Johnson', 'hashed_password_here', '5551234567', NULL, NULL);
SELECT changes();

INSERT INTO "Users" ("UserId", "CreatedAt", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "PhoneNumber", "ProfilePicture", "ProfilePictureContentType")
VALUES (2, '2024-01-01 00:00:00', 'customer@example.com', 'John', 1, 'Doe', 'hashed_password_here', '5559876543', NULL, NULL);
SELECT changes();



INSERT INTO "Queues" ("QueueId", "CreatedAt", "CurrentQueueSize", "Description", "EstimatedWaitTimePerPerson", "IsActive", "MaxCapacity", "QRCodeData", "QRCodeImageUrl", "QueueName", "Status", "TotalServedToday", "UpdatedAt", "UserId")
VALUES (1, '2024-01-01 00:00:00', 0, 'Main service queue for morning hours', 5, 1, 50, 'HIVEQ_QUEUE_1', NULL, 'Morning Service', 'Active', 0, '2024-01-01 00:00:00', 1);
SELECT changes();



CREATE INDEX "IX_Notifications_QueueEntryId" ON "Notifications" ("QueueEntryId");


CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");


CREATE INDEX "IX_QueueEntries_QueueId" ON "QueueEntries" ("QueueId");


CREATE INDEX "IX_QueueEntries_UserId" ON "QueueEntries" ("UserId");


CREATE UNIQUE INDEX "IX_QueueHistories_QueueEntryId" ON "QueueHistories" ("QueueEntryId");


CREATE INDEX "IX_QueueHistories_QueueId" ON "QueueHistories" ("QueueId");


CREATE INDEX "IX_QueueHistories_UserId" ON "QueueHistories" ("UserId");


CREATE UNIQUE INDEX "IX_Queues_QRCodeData" ON "Queues" ("QRCodeData");


CREATE INDEX "IX_Queues_UserId" ON "Queues" ("UserId");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


