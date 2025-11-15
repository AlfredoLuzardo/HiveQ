# HiveQ Database Schema - Visual Guide

## Entity Relationship Diagram (Text Format)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                  USERS                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ PK  UserId              INT                                             │
│ UK  Email               VARCHAR(255)                                    │
│     PasswordHash        VARCHAR(255)                                    │
│     PhoneNumber         VARCHAR(20)                                     │
│     FirstName           VARCHAR(100)                                    │
│     LastName            VARCHAR(100)                                    │
│     UserType            VARCHAR(50)     [Customer|CompanyOwner]        │
│     CreatedAt           DATETIME                                        │
│     IsActive            BOOLEAN                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ 1:1 (creates)
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                                COMPANIES                                │
├─────────────────────────────────────────────────────────────────────────┤
│ PK  CompanyId           INT                                             │
│ FK  UserId              INT          → Users.UserId                     │
│ UK  CompanyName         VARCHAR(200)                                    │
│     Description         TEXT                                            │
│     Address             VARCHAR(500)                                    │
│     PhoneNumber         VARCHAR(20)                                     │
│     Category            VARCHAR(100)                                    │
│     LogoUrl             VARCHAR(500)                                    │
│     IsVerified          BOOLEAN                                         │
│     CreatedAt           DATETIME                                        │
│     IsActive            BOOLEAN                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ 1:Many (manages)
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                                 QUEUES                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ PK  QueueId             INT                                             │
│ FK  CompanyId           INT          → Companies.CompanyId              │
│     QueueName           VARCHAR(200)                                    │
│     Description         TEXT                                            │
│ UK  QRCodeData          VARCHAR(500)                                    │
│     QRCodeImageUrl      VARCHAR(500)                                    │
│     Status              VARCHAR(50)  [Active|Paused|Closed]            │
│     MaxCapacity         INT                                             │
│     EstimatedWaitTimePerPerson  INT                                     │
│     CurrentQueueSize    INT                                             │
│     TotalServedToday    INT                                             │
│     CreatedAt           DATETIME                                        │
│     UpdatedAt           DATETIME                                        │
│     IsActive            BOOLEAN                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ 1:Many (contains)
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                             QUEUE ENTRIES                               │
├─────────────────────────────────────────────────────────────────────────┤
│ PK  QueueEntryId        INT                                             │
│ FK  QueueId             INT          → Queues.QueueId                   │
│ FK  UserId              INT          → Users.UserId                     │
│     PositionNumber      INT                                             │
│     Status              VARCHAR(50)  [Waiting|Notified|Served|...]     │
│     JoinedAt            DATETIME                                        │
│     NotifiedAt          DATETIME     (nullable)                         │
│     ServedAt            DATETIME     (nullable)                         │
│     EstimatedWaitTime   INT                                             │
│     NotificationPreference VARCHAR(50) [SMS|Email|Both]                │
│     Notes               TEXT                                            │
└─────────────────────────────────────────────────────────────────────────┘
                    │                               │
                    │ 1:Many                        │ 1:1 (optional)
                    │ (triggers)                    │ (archives to)
                    ↓                               ↓
    ┌───────────────────────────┐     ┌────────────────────────────────┐
    │      NOTIFICATIONS        │     │       QUEUE HISTORY            │
    ├───────────────────────────┤     ├────────────────────────────────┤
    │ PK NotificationId    INT  │     │ PK HistoryId           INT     │
    │ FK QueueEntryId      INT  │     │ FK QueueId             INT     │
    │ FK UserId            INT  │     │ FK UserId              INT     │
    │    Type         VARCHAR   │     │ FK QueueEntryId        INT     │
    │    Channel      VARCHAR   │     │    JoinedAt         DATETIME   │
    │    Status       VARCHAR   │     │    ServedAt         DATETIME   │
    │    Message      TEXT      │     │    WaitTime         INT        │
    │    SentAt       DATETIME  │     │    Status           VARCHAR    │
    │    CreatedAt    DATETIME  │     │    Date             DATE       │
    └───────────────────────────┘     └────────────────────────────────┘
```

## Relationship Details

### 1. Users → Companies (One-to-One)
- **Cardinality**: 1:1
- **Foreign Key**: Companies.UserId → Users.UserId
- **Delete Behavior**: CASCADE (deleting user deletes their company)
- **Business Logic**: Each company owner (user) can create one company

### 2. Companies → Queues (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: Queues.CompanyId → Companies.CompanyId
- **Delete Behavior**: CASCADE (deleting company deletes all its queues)
- **Business Logic**: A company can manage multiple queues

### 3. Queues → QueueEntries (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: QueueEntries.QueueId → Queues.QueueId
- **Delete Behavior**: CASCADE (deleting queue deletes all entries)
- **Business Logic**: A queue contains multiple customer entries

### 4. Users → QueueEntries (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: QueueEntries.UserId → Users.UserId
- **Delete Behavior**: RESTRICT (cannot delete user if they have active entries)
- **Business Logic**: A customer can join multiple queues

### 5. QueueEntries → Notifications (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: Notifications.QueueEntryId → QueueEntries.QueueEntryId
- **Delete Behavior**: CASCADE (deleting entry deletes notifications)
- **Business Logic**: Each queue entry can trigger multiple notifications

### 6. Users → Notifications (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: Notifications.UserId → Users.UserId
- **Delete Behavior**: RESTRICT (cannot delete user with notifications)
- **Business Logic**: A user receives multiple notifications

### 7. QueueEntries → QueueHistory (One-to-One, Optional)
- **Cardinality**: 1:0..1
- **Foreign Key**: QueueHistory.QueueEntryId → QueueEntries.QueueEntryId
- **Delete Behavior**: SET NULL (history preserved if entry deleted)
- **Business Logic**: Completed entries are archived for analytics

### 8. Queues → QueueHistory (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: QueueHistory.QueueId → Queues.QueueId
- **Delete Behavior**: CASCADE
- **Business Logic**: Queue maintains historical records

### 9. Users → QueueHistory (One-to-Many)
- **Cardinality**: 1:N
- **Foreign Key**: QueueHistory.UserId → Users.UserId
- **Delete Behavior**: RESTRICT
- **Business Logic**: User's historical data is preserved

## Data Flow Examples

### Customer Joins Queue
```
1. Customer (User) scans QR code
2. System finds Queue by QRCodeData
3. Creates new QueueEntry
   - Links to Queue (QueueId)
   - Links to User (UserId)
   - Assigns PositionNumber
   - Calculates EstimatedWaitTime
4. Queue.CurrentQueueSize increments
5. Optional: Create Notification
```

### Customer Gets Served
```
1. Company marks QueueEntry as "Served"
2. Updates ServedAt timestamp
3. Creates QueueHistory record
   - Archives entry details
   - Records WaitTime
4. Queue.CurrentQueueSize decrements
5. Queue.TotalServedToday increments
6. Advance all remaining positions
```

### Notification Flow
```
1. Queue position changes (trigger)
2. System creates Notification
   - Links to QueueEntry
   - Links to User
   - Sets Type (YourTurn, QueueUpdate, Reminder)
   - Sets Channel (SMS, Email)
3. Notification service sends message
4. Updates Notification.Status (Sent/Failed)
5. Records SentAt timestamp
```

## Index Strategy

### Unique Indexes (UK)
- **Users.Email** - Prevents duplicate accounts
- **Companies.CompanyName** - Ensures unique company names
- **Queues.QRCodeData** - Each queue has unique QR code

### Performance Indexes (Recommended)
```sql
-- Find active queues quickly
CREATE INDEX IX_Queues_Status ON Queues(Status);
CREATE INDEX IX_Queues_IsActive ON Queues(IsActive);

-- Find user's entries quickly
CREATE INDEX IX_QueueEntries_UserId_Status ON QueueEntries(UserId, Status);

-- Find queue entries by position
CREATE INDEX IX_QueueEntries_QueueId_Position ON QueueEntries(QueueId, PositionNumber);

-- Analytics queries
CREATE INDEX IX_QueueHistory_Date ON QueueHistory(Date);
CREATE INDEX IX_QueueHistory_QueueId_Date ON QueueHistory(QueueId, Date);

-- Notification lookups
CREATE INDEX IX_Notifications_Status ON Notifications(Status);
CREATE INDEX IX_Notifications_UserId_CreatedAt ON Notifications(UserId, CreatedAt);
```

## Status Field Values

### QueueEntry.Status
- **Waiting** - Customer is in queue
- **Notified** - Customer has been notified it's their turn
- **Served** - Customer has been served
- **Cancelled** - Customer cancelled their position
- **NoShow** - Customer didn't show up when called

### Queue.Status
- **Active** - Queue is accepting new customers
- **Paused** - Queue temporarily not accepting new customers
- **Closed** - Queue is closed (end of day)

### Notification.Status
- **Pending** - Notification created but not sent
- **Sent** - Notification successfully sent
- **Failed** - Notification failed to send
- **Delivered** - Notification confirmed delivered (for SMS)

### QueueHistory.Status
- **Completed** - Customer was successfully served
- **Cancelled** - Customer cancelled before being served
- **NoShow** - Customer didn't show when called

## UserType Values

### User.UserType
- **Customer** - Regular customer who joins queues
- **CompanyOwner** - Business owner who manages queues

## Sample Data Queries

### Get Queue with Current Entries
```sql
SELECT q.QueueName, q.CurrentQueueSize, 
       qe.PositionNumber, u.FirstName, u.LastName
FROM Queues q
LEFT JOIN QueueEntries qe ON q.QueueId = qe.QueueId
LEFT JOIN Users u ON qe.UserId = u.UserId
WHERE q.QueueId = 1 
  AND qe.Status = 'Waiting'
ORDER BY qe.PositionNumber;
```

### Get Company Analytics
```sql
SELECT q.QueueName, 
       COUNT(qh.HistoryId) as TotalServed,
       AVG(qh.WaitTime) as AvgWaitTime
FROM Queues q
LEFT JOIN QueueHistory qh ON q.QueueId = qh.QueueId
WHERE q.CompanyId = 1
  AND qh.Date >= '2024-01-01'
GROUP BY q.QueueId, q.QueueName;
```

### Get User's Active Queue Positions
```sql
SELECT q.QueueName, c.CompanyName,
       qe.PositionNumber, qe.EstimatedWaitTime
FROM QueueEntries qe
JOIN Queues q ON qe.QueueId = q.QueueId
JOIN Companies c ON q.CompanyId = c.CompanyId
WHERE qe.UserId = 1
  AND qe.Status = 'Waiting'
ORDER BY qe.JoinedAt DESC;
```
