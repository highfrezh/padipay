@REM Creating the project commands
dotnet new mvc -n PadiPay
cd PadiPay
cd PadiPay

@REM Installing dependencies
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

@REM Making Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

@REM Scaffold Identity's UI
dotnet tool install -g dotnet-aspnet-codegenerator
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
dotnet add package Microsoft.AspNetCore.Identity.UI
dotnet aspnet-codegenerator identity -dc PadiPay.Data.ApplicationDbContext --files "Account.Register;Account.Login;Account.Logout"