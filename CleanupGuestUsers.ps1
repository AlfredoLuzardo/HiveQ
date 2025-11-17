# PowerShell script to cleanup guest user accounts
# Usage: .\CleanupGuestUsers.ps1

$dbPath = ".\hiveq.db"

Write-Host "=== Guest User Cleanup Tool ===" -ForegroundColor Cyan
Write-Host ""

# Check if sqlite3 is available
$sqlite3Path = (Get-Command sqlite3 -ErrorAction SilentlyContinue).Source

if (-not $sqlite3Path) {
    Write-Host "SQLite3 not found. Please install it first or use DB Browser for SQLite." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: Run these SQL commands in DB Browser:" -ForegroundColor Green
    Write-Host ""
    Write-Host "-- View all guest users:" -ForegroundColor White
    Write-Host "SELECT UserId, Email, FirstName, LastName, CreatedAt FROM Users WHERE PasswordHash = 'GUEST_USER';" -ForegroundColor Gray
    Write-Host ""
    Write-Host "-- Delete guest users with no active queue entries:" -ForegroundColor White
    Write-Host @"
DELETE FROM Users 
WHERE PasswordHash = 'GUEST_USER' 
AND UserId NOT IN (
    SELECT DISTINCT UserId 
    FROM QueueEntries 
    WHERE Status IN ('Waiting', 'Notified')
);
"@ -ForegroundColor Gray
    Write-Host ""
    Write-Host "-- Delete ALL guest users (WARNING - also deletes their history):" -ForegroundColor White
    Write-Host "DELETE FROM Users WHERE PasswordHash = 'GUEST_USER';" -ForegroundColor Gray
    Write-Host ""
    exit
}

# Show guest users
Write-Host "Current Guest Users:" -ForegroundColor Green
& sqlite3 $dbPath "SELECT UserId, Email, FirstName, LastName, CreatedAt FROM Users WHERE PasswordHash = 'GUEST_USER';"
Write-Host ""

$count = & sqlite3 $dbPath "SELECT COUNT(*) FROM Users WHERE PasswordHash = 'GUEST_USER';"
Write-Host "Total guest users: $count" -ForegroundColor Yellow
Write-Host ""

# Show active vs inactive
$activeGuests = & sqlite3 $dbPath @"
SELECT COUNT(DISTINCT u.UserId)
FROM Users u
INNER JOIN QueueEntries qe ON u.UserId = qe.UserId
WHERE u.PasswordHash = 'GUEST_USER'
AND qe.Status IN ('Waiting', 'Notified');
"@

$inactiveGuests = $count - $activeGuests
Write-Host "Active (in queue): $activeGuests" -ForegroundColor Cyan
Write-Host "Inactive (can be deleted): $inactiveGuests" -ForegroundColor Yellow
Write-Host ""

if ($inactiveGuests -eq 0) {
    Write-Host "No inactive guest users to clean up!" -ForegroundColor Green
    exit
}

# Ask what to delete
Write-Host "What would you like to delete?" -ForegroundColor Cyan
Write-Host "1. Delete inactive guest users only (safe - keeps users still in queues)"
Write-Host "2. Delete ALL guest users (WARNING - includes active users and history)"
Write-Host "3. Exit"
Write-Host ""

$choice = Read-Host "Enter your choice (1-3)"

switch ($choice) {
    "1" {
        $confirm = Read-Host "Delete $inactiveGuests inactive guest users? (yes/no)"
        if ($confirm -eq "yes") {
            & sqlite3 $dbPath @"
DELETE FROM Users 
WHERE PasswordHash = 'GUEST_USER' 
AND UserId NOT IN (
    SELECT DISTINCT UserId 
    FROM QueueEntries 
    WHERE Status IN ('Waiting', 'Notified')
);
"@
            Write-Host "Inactive guest users deleted!" -ForegroundColor Green
        }
    }
    "2" {
        $confirm = Read-Host "WARNING: This will delete ALL guest users including their queue history! Type 'DELETE ALL GUESTS' to confirm"
        if ($confirm -eq "DELETE ALL GUESTS") {
            & sqlite3 $dbPath "DELETE FROM Users WHERE PasswordHash = 'GUEST_USER';"
            Write-Host "All guest users deleted!" -ForegroundColor Red
        }
    }
    "3" {
        Write-Host "Exiting..." -ForegroundColor Yellow
        exit
    }
    default {
        Write-Host "Invalid choice!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Remaining Guest Users:" -ForegroundColor Green
& sqlite3 $dbPath "SELECT UserId, Email, FirstName, LastName FROM Users WHERE PasswordHash = 'GUEST_USER';"
