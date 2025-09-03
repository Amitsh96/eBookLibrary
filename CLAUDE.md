# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 MVC web application for an eBook library system. The application provides book browsing, borrowing, purchasing, user management, and administrative features.

## Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the application
dotnet run --project eBookLibrary

# Run with specific profile (https or http)
dotnet run --project eBookLibrary --launch-profile https
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project eBookLibrary

# Update database
dotnet ef database update --project eBookLibrary

# Drop database (development only)
dotnet ef database drop --project eBookLibrary
```

### Package Management
```bash
# Restore packages
dotnet restore

# Add package
dotnet add eBookLibrary package <PackageName>
```

## Architecture

### Core Components

- **LibraryContext** (Models/LibraryContext.cs): Entity Framework DbContext managing all database entities
- **Controllers**: MVC controllers handling HTTP requests
  - HomeController: Landing page and general features
  - UserController: User-specific operations (borrowing, purchasing, library)
  - AdminController: Administrative functions
  - AccountController: Authentication and registration
- **Services**: Background services and utilities
  - BorrowReminderService: Automated reminder emails
  - RemoveExpiredBooksService: Cleanup of expired borrowed books
  - UpdateWaitingListService: Waiting list management
  - EmailService: Email notifications

### Database Models

Primary entities: User, Book, BorrowedBook, PurchasedBook, WaitingList, Rating, Review, Notification, ServiceFeedback, ShoppingCart

### Configuration

- **Connection String**: Uses SQL Server LocalDB (`LibraryContext` in appsettings.json)
- **Authentication**: Cookie-based authentication with 24-hour expiry
- **Session**: 30-minute timeout with secure cookie settings
- **Security**: CORS enabled for localhost origins, antiforgery protection, security headers middleware

### Key Features

- Book borrowing system with 14-day periods and 5-book limit per user
- Waiting list functionality for unavailable books
- Purchase system with fake payment gateway
- Email notifications for various events
- Admin panel for book and user management
- Rating and review system
- Shopping cart functionality

### URLs

- Development HTTPS: https://localhost:44365
- Development HTTP: http://localhost:5103
- IIS Express: https://localhost:44365

## File Structure

- `/Controllers/`: MVC controllers
- `/Models/`: Entity models and view models
- `/Views/`: Razor views organized by controller
- `/Services/`: Background services and utilities
- `/Migrations/`: Entity Framework database migrations
- `/wwwroot/`: Static files (CSS, JS, images)
- `/Properties/`: Launch settings and service dependencies