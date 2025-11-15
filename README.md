# HiveQ - Virtual Queue Management System

HiveQ is an online queuing system that enables companies to create virtual queues that customers can join remotely by scanning QR codes, eliminating the need for physical waiting in lines.

## Project Overview

This is a medium-fidelity prototype built using **ASP.NET Core MVC** with **SQLite** database. The system provides:

- **Customer-facing features**: Join queues remotely, track position in real-time
- **Company management features**: Create and manage queues, view dashboards, track analytics
- **Queue management**: Real-time queue updates, notifications, and position tracking

## Technology Stack

- **Framework**: ASP.NET Core 9.0 MVC
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5, Razor Views
- **Architecture**: Model-View-Controller (MVC) pattern

## Project Structure

```
HiveQ/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs
│   ├── CreateQueueController.cs
│   ├── HomeController.cs
│   ├── ManageQueuesController.cs
│   └── ProfileController.cs
├── Models/              # Data models and DbContext
│   ├── ApplicationDbContext.cs
│   ├── Company.cs
│   ├── Queue.cs
│   ├── QueueEntry.cs
│   ├── QueueHistory.cs
│   ├── User.cs
│   ├── Notification.cs
│   └── ErrorViewModel.cs
├── Views/               # Razor views
│   ├── Account/
│   ├── CreateQueue/
│   ├── Home/
│   ├── ManageQueues/
│   ├── Profile/
│   └── Shared/
├── Migrations/          # EF Core database migrations
├── wwwroot/            # Static files (CSS, JS, images)
└── appsettings.json    # Configuration including DB connection
```

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A code editor (Visual Studio, VS Code, or Rider)

### Installation

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd HiveQ
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   
   This will create the `hiveq.db` SQLite database file with all necessary tables.

### Running the Application

#### Option 1: Using dotnet CLI
```bash
dotnet run
```

#### Option 2: Using dotnet watch (with hot reload)
```bash
dotnet watch
```

The application will start and be available at:
- HTTPS: `https://localhost:7XXX` (port may vary)
- HTTP: `http://localhost:5XXX` (port may vary)

Check the console output for the exact URLs.

## Database

The project uses **SQLite** for data persistence with **Entity Framework Core** for ORM.

### Database Schema

- **Users**: Customer accounts with authentication info
- **Companies**: Business accounts managing queues
- **Queues**: Virtual queues created by companies
- **QueueEntries**: Individual entries in queues (join events)
- **QueueHistory**: Historical record of queue activities
- **Notifications**: User notifications for queue updates

### Database Commands

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

For more detailed database information, see:
- [DATABASE_SETUP.md](DATABASE_SETUP.md) - Setup and configuration
- [DATABASE_QUICK_REFERENCE.md](DATABASE_QUICK_REFERENCE.md) - Quick reference guide
- [DATABASE_SCHEMA_VISUAL.md](DATABASE_SCHEMA_VISUAL.md) - Visual schema documentation

## Features

### For Customers
- Browse available queues
- Join queues remotely via QR code or web interface
- Track real-time position in queue
- Receive notifications when turn approaches
- View queue history

### For Companies
- Create and manage multiple queues
- Real-time queue monitoring dashboard
- Add/remove customers from queue
- View analytics and statistics
- Manage company profile

## Development Notes

- The project follows MVC architectural pattern for separation of concerns
- Bootstrap 5 is used for responsive UI design
- Controllers are organized by feature area (Account, Queue, Company)
- Views use Razor syntax for dynamic content rendering

## Important Files

- `Program.cs` - Application entry point and service configuration
- `appsettings.json` - Configuration including database connection string
- `.gitignore` - Excludes build artifacts and database files from version control

## Contributing

This is an academic project. For development:

1. Create a new branch for features
2. Make changes and test locally
3. Ensure migrations are created for any model changes
4. Submit pull request for review

## License

This is an academic project for educational purposes.

## Contact

For questions or issues, please contact the project team.
