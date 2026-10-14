# Employee Management System

A full-stack Employee Management System built with **ASP.NET Core Web API, Angular, and SQL Server**. This project demonstrates backend API development, frontend integration, authentication, role-based access control, leave management, and automated testing.

## Overview

The Employee Management System provides a centralized application for managing employee information and employee leave requests. It includes separate experiences for administrators and employees, with protected pages and role-specific functionality.

## Tech Stack

| Category | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8) |
| Frontend | Angular 22, TypeScript, HTML, CSS |
| Database | Microsoft SQL Server |
| Authentication | JWT-based authentication |
| API Testing | Swagger / OpenAPI |
| Automated Testing | .NET test project, SQL Server integration tests |
| Version Control | Git, GitHub |

## Features

### Authentication and Authorization
- User login and logout
- JWT-based authentication
- Role-based access for Admin and Employee
- Protected frontend routes
- Password change functionality

### Employee Management
- View employee records
- Add new employees
- View employee details
- Edit employee information
- Department management

### Leave Management
- Submit leave requests
- View leave request history
- Review and approve or reject employee requests
- Manage leave entitlements
- Track leave balances and used days
- View leave approval history
- Configure public holidays

### Reliability and Testing
- Backend unit and integration tests
- SQL Server concurrency tests
- Input and business-rule validation
- API error handling

## System Architecture

┌──────────────────────────┐
│     Angular Frontend     │
│                          │
│ Login / Employee / Admin │
└────────────┬─────────────┘
             │ HTTP / HTTPS
             ▼
┌──────────────────────────┐
│ ASP.NET Core Web API     │
│                          │
│ Controllers / Services   │
│ JWT / Authorization      │
│ Business Rules           │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│      SQL Server          │
│                          │
│ Employee / Department    │
│ Leave / Entitlement      │
└──────────────────────────┘

## Project Structure

EmployeeManagement.Api/
├── Controllers/
├── Services/
├── Models/
├── Data/
├── EmployeeManagement.Tests/
└── EmployeeManagement.Client/
    └── src/
        └── app/
            ├── core/
            └── features/
                ├── admin/
                ├── auth/
                ├── departments/
                ├── employees/
                ├── leave/
                └── public-holidays/

*Adjust the structure above to match the actual folders in the repository.*

## Getting Started

### Prerequisites

Install the following:

- .NET 8 SDK
- Node.js and npm compatible with the Angular project
- Angular CLI compatible with the installed Angular version
- Microsoft SQL Server
- SQL Server Management Studio (optional, for database management)

### 1. Clone the repository

git clone <YOUR_GITHUB_REPOSITORY_URL>
cd EmployeeManagement.Api

### 2. Configure the database

Configure the SQL Server connection string for your local environment.

Before running the application:

1. Create or configure the development database.
2. Set the connection string using the project's supported configuration method.
3. Apply the existing Entity Framework Core migrations, if applicable.
4. Confirm the API can connect to the database.

Do not commit database passwords, JWT signing secrets, or other credentials to GitHub.

### 3. Run the backend

Open a terminal in the backend project directory and run:

dotnet restore
dotnet run

If the repository contains a solution with multiple projects, run the API project explicitly using `dotnet run --project <API_PROJECT_PATH>`.

Open the Swagger URL printed in the terminal to inspect and test the API.

### 4. Run the Angular frontend

Open a separate terminal:

cd EmployeeManagement.Client
npm install
ng serve

Open the application:

http://localhost:4200/login

Ensure the backend is running and the API URL and local HTTPS certificate configuration match the frontend settings.

### 5. Run automated tests

From the repository root:

dotnet test .\EmployeeManagement.Tests\EmployeeManagement.Tests.csproj

Latest verified result:

**57 passed · 0 failed · 0 skipped**

The tests include SQL Server concurrency scenarios. Test results may change as the project evolves.

### 6. Build the frontend

cd EmployeeManagement.Client
ng build

The Angular production build completed successfully in the latest local verification. Some component CSS budget warnings remain.

## Screenshots

Add screenshots of the following pages to a `docs/screenshots/` folder and link them here:

- Login page
- Admin dashboard
- Employee dashboard
- Employee list and employee details
- Leave application form
- Leave approval page
- Leave entitlement and balance

Example:

![Admin Dashboard](docs/screenshots/admin-dashboard.png)

Use fictional employee information in screenshots.

## Future Improvements

- Deploy the frontend and backend to a cloud hosting environment
- Configure production database connectivity and secure secrets
- Add a publicly accessible demo environment
- Document database relationships and API endpoints
- Add a CI pipeline to build the frontend and run automated tests

## Author

Developed as a full-stack portfolio project to demonstrate practical skills in .NET backend development, Angular, SQL Server, authentication, business logic, and automated testing.
