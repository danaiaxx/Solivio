🚀 Solivio ASP.NET 9
Solivio is a modern web application built with ASP.NET Core 9, designed to provide a simple platform for solo travelers to share their journeys to users within the app.

🛠️ Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or Visual Studio 2022 Preview

🚀 Getting Started
- 🖥️ Open Visual Studio 2022
- Launch Visual Studio 2022 on your computer.

📂 Clone the repository
- In Visual Studio, go to File > Clone Repository
- Paste the repo URL:
https://github.com/danaiaxx/Solivio.git
- Choose your local folder to clone into
- Click Clone 🔄

📂 Open the solution
- Visual Studio will open the solution (Solivio.sln) automatically.
- If not, manually open the .sln file from the cloned folder.

⚙️ Configure the database connection
- Open appsettings.json and update the connection string:
"Server=(localdb)\\mssqllocaldb;Database=SolivioDb;Trusted_Connection=True;"

🛠️ Apply database migrations
- Open the Package Manager Console (Tools > NuGet Package Manager > Package Manager Console)
- Run: Update-Database

▶️ Run the application
- Press F5 or click the Start button in Visual Studio.

🌐 Access the app
- Open your browser and go to:
https://localhost:5039

🔑 Sample Credentials
- Use the following to log in and test the application:
1. Username: _danaiaxx
2. Password: 12345
3. Security answer: Cebu

🖥️ Solivio App Demo
- ![Demo](Solivio.gif)
