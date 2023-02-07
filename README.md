# News Website

## Introduction
A modern and dynamic news website built using the latest technologies, including .NET Core 3.1, ASP.NET Core, and SQL Server 2019. This website was developed using Visual Studio 2022, ensuring a seamless and efficient development process. The website features a user-friendly interface and is optimized for performance and scalability. Whether you're a seasoned developer or just starting out, this project is the perfect starting point for building your own news website.

## Prerequisites
- .NET Core 3.1 SDK
- Visual Studio 2022
- SQL Server 2019

## Getting Started
1. Clone the repository

git clone https://github.com/rasoolfetrati/TechUpdate.git



2. Open the solution file in Visual Studio 2022
3. Restore the NuGet packages by right-clicking the solution and selecting "Restore NuGet Packages"
4. Build the solution
5. Update the connection string in the `appsettings.json` file to connect to your SQL Server database.
6. Run the following commands in the Package Manager Console to apply migrations to the database:

dotnet ef migrations add InitialCreate
<br>
dotnet ef database update

7. Run the application by pressing `F5` or clicking the "Start" button in Visual Studio.

## Built With
- .NET Core 3.1
- ASP.NET Core
- SQL Server 2019
- Visual Studio 2022

## Contributing
1. Fork the repository
2. Create your feature branch (`git checkout -b feature/[your-feature-name]`)
3. Commit your changes (`git commit -m 'Add [your-feature-name]'`)
4. Push to the branch (`git push origin feature/[your-feature-name]`)
5. Create a new Pull Request


