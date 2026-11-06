# AssetManagement

AssetManagement is a web-based asset management application designed to manage company IT equipment throughout its lifecycle.

The system provides centralized asset tracking, store assignment, inventory management, service case handling, and package-based asset transportation between locations.

## Features

### Asset Management

The application provides centralized management of company assets.

Main capabilities include:

- Create and manage assets
- Track asset name and serial number
- Assign assets to stores and locations
- Track the current status of each asset
- Search and filter assets
- View detailed asset information
- Maintain asset history and operational state

Asset operations are protected by backend business rules to prevent invalid or conflicting changes.

### Store Management

Assets can be assigned to stores managed by the system.

Store information can be used throughout the application for:

- Asset location tracking
- Service operations
- Package source and destination selection
- Inventory workflows

Stores are persistent business entities and are not deleted as part of normal application workflows.

### Service Cases

The Service module provides a workflow for handling assets that require servicing or repair.

Users can:

1. Create a service case for an asset.
2. Track the service case status.
3. Associate the affected asset with the case.
4. Manage the service workflow independently from transportation.
5. Track external service-related information.

Service cases and packages are intentionally separate workflows.

Creating a service case does not automatically create a package. If transportation is required, the user creates the package separately.

### Packages

The package management module handles transportation of assets between locations.

Users can:

- Create packages
- Select source and destination
- Add multiple assets to a package
- Remove assets before dispatch
- Assign a courier
- Dispatch packages
- Receive packages
- Cancel packages when permitted
- Track package status

The package details view shows the assets contained in the package, including identifying information such as asset name and serial number.

The package list remains compact and displays summary information such as the number of assets contained in each package.

### Package Asset Selection

Assets can be added to packages through an asset selection interface.

The interface supports:

- Asset search
- Table-based asset display
- Multi-select
- Adding multiple assets at once
- Removing assets
- Prevention of invalid asset assignments
- Validation against package and asset state

Backend validation remains authoritative even when frontend filtering prevents invalid selections.

### Inventory

The application is designed to support inventory workflows for tracking physical assets across stores and other company locations.

Inventory functionality can be used to compare assets registered in the system with assets physically present at a location and identify discrepancies.

## Application Structure

The solution consists of a backend API and an Angular frontend.

### Backend

The backend is implemented with:

- C#
- .NET
- ASP.NET Core
- Entity Framework Core
- REST API endpoints
- Dependency Injection
- Repository-based data access
- Request/Handler-based application logic
- Transaction handling
- Concurrency-aware asset operations

Business operations such as package creation, dispatch, receipt, cancellation, courier assignment, and adding or removing assets are handled by dedicated application handlers.

This keeps business rules outside controllers and provides a clear separation between API, application, domain, and persistence responsibilities.

### Frontend

The frontend is implemented with:

- Angular
- TypeScript
- Angular Material
- HTML
- CSS

The UI uses Material components for a consistent user experience, including:

- Dialogs
- Snackbars
- Tables
- Chips
- Forms
- Search and filtering
- Multi-select controls
- Status indicators
- Navigation

## Main Modules

The application is organized around several business areas:

- Dashboard
- Assets
- Stores
- Inventory
- Administration
- Operations
  - Service
  - Packages

The Operations area acts as the entry point for workflows that operate on existing assets.

## Business Rules

The application enforces business rules on the backend rather than relying only on UI validation.

Examples include:

- Assets cannot be added to packages when their current state does not allow it.
- Duplicate asset assignments are prevented.
- Package modifications depend on the current package status.
- Asset operations are executed within controlled transactions.
- Concurrent operations on the same asset are protected.
- Service cases and package transportation remain separate workflows.
- Store records are treated as persistent entities.

## Error Handling

Backend validation errors are returned to the frontend and displayed to the user through the Angular UI.

User-facing descriptions and error messages are written in English to provide consistent application behavior.

## Architecture

The project follows a layered structure similar to:

```text
Frontend
   |
   v
ASP.NET Core API
   |
   v
Application / Handlers
   |
   v
Domain
   |
   v
Repositories / Entity Framework Core
   |
   v
Database
