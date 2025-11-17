# Queue Join System - Documentation

## Overview

The HiveQ application now supports a complete queue joining system that allows both **registered users** and **guest users** (without accounts) to join queues via QR codes or direct links.

## How It Works

### For Queue Owners (Creating Queues)

1. Navigate to `/CreateQueue`
2. Fill out the queue creation form
3. After creation, you'll receive:
   - A unique Queue ID
   - A shareable join link (e.g., `https://yoursite.com/JoinQueue?queueId=123`)
   - Instructions to generate a QR code

### For Customers (Joining Queues)

#### Option 1: Scan QR Code
- Queue owners can generate QR codes from the join link
- Customers scan the QR code with their phone
- They're taken directly to the join page

#### Option 2: Direct Link
- Queue owners share the join link directly
- Customers click the link to join

## Guest User System

### Problem Solved
We needed to allow people **without accounts** to join queues, but the database requires a `UserId` foreign key for queue entries.

### Solution: Auto-Generated Guest Users

When someone joins a queue without an account:

1. **Guest user is created automatically** with:
   - Email: Either provided email or `guest_{GUID}@hiveq.local`
   - Name: First and last name from the form
   - Phone: Optional, for SMS notifications
   - Password: Set to `"GUEST_USER"` (they cannot log in)
   - IsVerified: `false`

2. **Queue entry is created** linking the guest user to the queue

3. **User receives a position tracking page** with:
   - Current position in queue
   - Estimated wait time
   - Ability to leave the queue
   - Auto-refresh every 30 seconds

### Benefits
- ✅ Works with existing database schema (no migration needed)
- ✅ Maintains referential integrity
- ✅ Allows notifications to guest users
- ✅ Guest users can be identified for future visits
- ✅ Easy to clean up guest users later

## Features

### Join Queue Page (`/JoinQueue?queueId={id}`)
- Shows queue information (name, description, current size)
- Displays estimated wait time
- Form for entering customer information:
  - First Name (required)
  - Last Name (required)
  - Email (optional, required for email notifications)
  - Phone Number (optional, required for SMS notifications)
  - Notification Preference (None, Email, SMS, Both)

### Position Tracking Page
- Real-time position display
- Estimated wait time
- Auto-refresh every 30 seconds
- Leave queue button
- Bookmark-friendly URL to check position anytime

### Validations
- Queue capacity checks
- Queue status checks (Open/Paused/Closed)
- Required field validation based on notification preferences
- Duplicate prevention for returning guests (by email)

## Database Structure

### QueueEntry
```csharp
- QueueEntryId (PK)
- QueueId (FK to Queue)
- UserId (FK to User) ← Can be guest user
- PositionNumber
- Status (Waiting, Notified, Served, Cancelled, NoShow)
- JoinedAt
- EstimatedWaitTime
- NotificationPreference
```

### Guest User Example
```csharp
{
    UserId: 123,
    Email: "john.doe@example.com" or "guest_abc123@hiveq.local",
    FirstName: "John",
    LastName: "Doe",
    PhoneNumber: "5551234567",
    PasswordHash: "GUEST_USER",
    IsVerified: false
}
```

## API Endpoints

### GET `/JoinQueue/Index?queueId={id}`
Displays the join queue form

### GET `/JoinQueue/Index?code={qrCodeData}`
Displays the join queue form (QR code variant)

### POST `/JoinQueue/Join`
Processes the queue join request
- Creates guest user if needed
- Creates queue entry
- Updates queue statistics
- Redirects to position page

### GET `/JoinQueue/ViewPosition?queueEntryId={id}`
Displays current position in queue

### POST `/JoinQueue/Leave?queueEntryId={id}`
Removes user from queue

## Testing

To test the system:

1. **Create a queue**:
   ```
   Navigate to /CreateQueue
   Fill out form and submit
   Copy the join link
   ```

2. **Join as a guest**:
   ```
   Visit the join link
   Enter name: "Test User"
   Leave email/phone blank
   Select "No notifications"
   Submit
   ```

3. **View position**:
   ```
   After joining, you'll see your position
   Bookmark the page
   Refresh to see updates
   ```

4. **Check database**:
   ```sql
   -- View guest users
   SELECT * FROM Users WHERE PasswordHash = 'GUEST_USER';
   
   -- View queue entries
   SELECT * FROM QueueEntries;
   ```

## Future Enhancements

1. **QR Code Generation**: Integrate a QR code library (e.g., QRCoder) to generate actual QR code images
2. **Guest User Cleanup**: Periodic job to delete old guest users
3. **Real-time Updates**: WebSockets for live position updates without refresh
4. **SMS Integration**: Actual SMS notification sending
5. **Guest to Registered**: Allow guests to claim their history by registering

## Security Considerations

- Guest users cannot log in (PasswordHash = "GUEST_USER")
- Unique email constraint prevents duplicate guest accounts
- Position page URLs are not guessable (uses QueueEntryId)
- No sensitive data exposed on public pages
