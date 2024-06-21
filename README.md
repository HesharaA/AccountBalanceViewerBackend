# AccountBalanceViewer

ASP .NET Web API backend of the [Account Balance Viewer](https://github.com/HesharaA/AccountBalanceViewerFrontEnd)

## Prerequisites

- .NET 8
- SQL Server
- Visual Studio
- VSCode + extensions
  - .NET Extension Pack
  - .NET Install Tool
  - C#
  - C# Dev Kit
  - C# Extensions 

## Installation

1. Clone the repository:
   - Run `git clone https://github.com/HesharaA/AccountBalanceViewerBackend.git`
   - Run `cd AccountBalanceViewerBackend`
2. Install dependencies:
   - Run `dotnet restore`

## Start development server

1. Ensure a local database has been created using SQL Server.
2. Configuring the Database Connection
   1. Open the appsettings.json file located in the root directory of your project.
   2. Locate the DefaultConnection string.
   3. Replace {SERVER_NAME} and {DATABASE_NAME} with your actual server name and database name, respectively.
   - Example 
   >`"DefaultConnection":"Data Source={SERVER_NAME}\\SQLEXPRESS;Initial Catalog={DATABASE_NAME};Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"`
3. Run `dotnet watch run`

## Build for production

Buld and publish using visual studio.



