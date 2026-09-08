# QueryMind AI

QueryMind AI is an AI-powered ASP.NET Core MVC application that lets users query a SQL Server database using simple English.

The application reads the database structure, sends the user’s question and relevant schema information to Google Gemini, generates a SQL Server query, validates it for safety, executes it, and presents the results in a dynamic table.

## Example

### User question

```text
Show all employees from the EmployeeMVC table.
```

### AI-generated SQL

```sql
SELECT TOP 100 *
FROM dbo.EmployeeMVC;
```

The application validates this query before allowing it to run.

## Main Features

* Ask database questions using natural language
* Automatic SQL Server schema detection
* Automatic table and column discovery
* AI-powered SQL generation using Google Gemini
* SQL syntax validation using Microsoft ScriptDom
* Blocks destructive operations such as `DELETE`, `DROP`, `UPDATE`, and `TRUNCATE`
* Blocks dangerous requests before sending them to AI
* Allows only a single read-only `SELECT` statement
* Prevents `SELECT INTO` queries
* Executes validated SQL using ADO.NET
* Displays results in a dynamic table
* Supports different result columns for different queries
* Limits displayed results to 100 rows
* Uses a 30-second query timeout
* Measures query execution time
* Handles duplicate column names
* Stores the Gemini API key securely using User Secrets
* Handles Gemini API errors and temporary service failures

## Application Workflow

```text
User enters a question
        ↓
Application reads the database schema
        ↓
Question safety validation
        ↓
Schema and question are sent to Gemini
        ↓
Gemini generates SQL
        ↓
SQL safety validation
        ↓
User reviews the generated SQL
        ↓
Safe SQL is executed
        ↓
Results are displayed dynamically
```

## Technologies Used

* ASP.NET Core MVC
* .NET 10
* C#
* SQL Server
* ADO.NET
* Google Gemini API
* REST API
* JSON
* Razor Views
* Bootstrap
* Dependency Injection
* Async/await
* Microsoft.Data.SqlClient
* Microsoft.SqlServer.TransactSql.ScriptDom
* Visual Studio User Secrets

## Project Structure

```text
QueryMindAI
├── Controllers
│   └── QueryController.cs
│
├── Interfaces
│   ├── IAiSqlGeneratorService.cs
│   ├── IDatabaseSchemaService.cs
│   ├── IQueryExecutionService.cs
│   ├── IQuestionSafetyValidator.cs
│   └── ISqlSafetyValidator.cs
│
├── Models
│   ├── QueryExecutionResult.cs
│   └── SqlValidationResult.cs
│
├── Services
│   ├── DatabaseSchemaService.cs
│   ├── GeminiSqlGeneratorService.cs
│   ├── QueryExecutionService.cs
│   ├── QuestionSafetyValidator.cs
│   └── SqlSafetyValidator.cs
│
├── ViewModels
│   └── QueryAssistantViewModel.cs
│
├── Views
│   └── Query
│       └── Index.cshtml
│
├── wwwroot
├── appsettings.json
└── Program.cs
```

## How It Works

### 1. Database schema detection

`DatabaseSchemaService` connects to SQL Server and reads metadata from:

```sql
INFORMATION_SCHEMA.TABLES
```

and:

```sql
INFORMATION_SCHEMA.COLUMNS
```

This provides Gemini with the actual table names, column names, data types, and nullable information required to generate accurate SQL.

### 2. Question validation

`QuestionSafetyValidator` checks the user’s original question before calling Gemini.

It blocks requests containing destructive operations such as:

```text
delete
drop
truncate
update
insert
alter
merge
execute
create
```

For example:

```text
Delete the EmployeeMVC table.
```

is rejected before it reaches Gemini.

### 3. Gemini integration

`GeminiSqlGeneratorService` sends the following information to the Gemini API:

* User’s natural-language question
* Detected database structure
* Instructions to generate SQL Server syntax
* Instructions to return only a read-only `SELECT` query

Gemini returns SQL text, and the service removes Markdown formatting before passing it to the controller.

### 4. SQL safety validation

`SqlSafetyValidator` uses Microsoft SQL Server ScriptDom to parse the generated SQL.

The validator ensures:

* The SQL syntax is valid
* Only one SQL batch exists
* Only one statement exists
* The statement is a `SELECT`
* `SELECT INTO` is not used

### 5. Query execution

After validation, `QueryExecutionService` executes the SQL using:

* `SqlConnection`
* `SqlCommand`
* `SqlDataReader`

The service dynamically reads the returned columns and rows, meaning it can display results from different tables without requiring a fixed entity model.

## Safety Design

AI-generated SQL must never be trusted without validation.

QueryMind AI uses multiple safety layers:

1. The user’s question is checked for destructive intent.
2. Gemini is instructed to generate only `SELECT` statements.
3. Generated SQL is parsed using SQL Server ScriptDom.
4. SQL is validated again immediately before execution.
5. Results are limited to 100 displayed rows.
6. Queries use a 30-second timeout.

For production usage, the database connection should also use a dedicated SQL Server account with read-only permissions.

## Getting Started

### Prerequisites

* Visual Studio
* .NET 10 SDK
* SQL Server
* Access to an existing SQL Server database
* Google Gemini API key

### NuGet Packages

Install the following packages:

```text
Microsoft.Data.SqlClient
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.SqlServer.TransactSql.ScriptDom
```

## Database Configuration

Add your database connection to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TargetDatabase": "Server=YOUR_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/models"
  }
}
```

Do not commit real server credentials or passwords to a public repository.

## Gemini API Configuration

Create a Gemini API key through Google AI Studio.

In Visual Studio:

1. Right-click the project.
2. Select **Manage User Secrets**.
3. Add the following configuration:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-3.6-flash"
  }
}
```

Never store the real API key directly in `appsettings.json`.

## Service Registration

The project services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<
    IDatabaseSchemaService,
    DatabaseSchemaService>();

builder.Services.AddHttpClient<
    IAiSqlGeneratorService,
    GeminiSqlGeneratorService>();

builder.Services.AddScoped<
    IQuestionSafetyValidator,
    QuestionSafetyValidator>();

builder.Services.AddScoped<
    ISqlSafetyValidator,
    SqlSafetyValidator>();

builder.Services.AddScoped<
    IQueryExecutionService,
    QueryExecutionService>();
```

## Running the Application

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Configure the SQL Server connection string.
4. Add the Gemini API key through User Secrets.
5. Restore the NuGet packages.
6. Build the solution.
7. Run the project.
8. Enter a database question.
9. Click **Generate SQL**.
10. Review the generated query.
11. Click **Execute Query** if the query passes validation.

## Example Questions

```text
Show all employees from the EmployeeMVC table.
```

```text
Show the first 10 employees ordered by salary.
```

```text
Count the number of employees in each department.
```

```text
Show employees who joined after January 2025.
```

The questions must reference tables and information that exist in the connected database.

## Current Limitations

* The application currently supports SQL Server only.
* Only read-only `SELECT` queries are allowed.
* AI output may occasionally require regeneration.
* A large database schema can increase API request size.
* Gemini availability and usage limits depend on the configured API account.
* Database structure should only be sent to Gemini when organizational policies permit it.

## Planned Improvements

* Searchable table selection
* Send only selected table schemas to Gemini
* Query history
* Saved and favourite queries
* User registration and login
* CSV and Excel export
* Query explanation in simple language
* Data visualization using charts
* Pagination
* Real-time query notifications
* Configurable database connections
* Administrative dashboard
* Improved schema relationship detection

## Security Notice

This project is intended for learning and controlled environments.

Before using it with a production or company database:

* Obtain permission to send schema information to an external AI provider.
* Use a dedicated read-only database account.
* Never expose connection strings or API keys.
* Apply authentication and authorization.
* Maintain query limits and timeouts.
* Review all generated SQL before execution.

## Author

**Titiksha Jangid**

B.Tech Artificial Intelligence and Machine Learning student with an interest in AI-powered applications, ASP.NET Core, SQL Server, and full-stack development.
