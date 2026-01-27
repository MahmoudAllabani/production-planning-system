\# Production Planning System (ASP.NET MVC + ML.NET)



Web-based production planning system developed with ASP.NET MVC and SQL Server.

Includes an ML.NET demand forecasting module integrated into the web application.



\## Features

\- Materials \& Stock management

\- Product / Recipe (BOM) management

\- Orders management

\- Maintenance tracking

\- Reporting / PDF export (Rotativa)

\- Demand forecasting (ML.NET model)



\## Tech Stack

\- ASP.NET MVC (.NET Framework 4.7.2)

\- Entity Framework (Database First)

\- SQL Server / LocalDB

\- ML.NET (model trained in C# Console app, exported and integrated into MVC)

\- Bootstrap + JS



\## Project Structure

\- `src/web` : ASP.NET MVC web application

\- `src/ml`  : C# Console project for training the ML model



\## Setup (Local)

1\. Open `src/web/MDK.sln` in Visual Studio

2\. Configure SQL Server (LocalDB is supported)

3\. Update connection string in `src/web/Web.config` if needed

4\. Run the database seed script: `database/seed.sql` (creates required admin settings)

5\. Run the project



\### Demo Login

\- Email: `demo@demo.com`

\- Password: `demo1234!`

> Password is stored as BCrypt hash in DB (`Ayarlar` table).



\## Notes

This project is a prototype / academic project and was not deployed for a real company.



