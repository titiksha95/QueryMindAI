using Microsoft.AspNetCore.Mvc;
using QueryMindAI.Interfaces;
using QueryMindAI.ViewModels;
using QueryMindAI.Models;

namespace QueryMindAI.Controllers
{
    public class QueryController : Controller
    {
        private readonly IDatabaseSchemaService
            _databaseSchemaService;
        private readonly IAiSqlGeneratorService
            _aiSqlGeneratorService;
        private readonly ISqlSafetyValidator
            _sqlSafetyValidator;
        private readonly IQueryExecutionService
            _queryExecutionService;
        private readonly IQuestionSafetyValidator
            _questionSafetyValidator;

        public QueryController(
             IDatabaseSchemaService databaseSchemaService,
             IAiSqlGeneratorService aiSqlGeneratorService,
             ISqlSafetyValidator sqlSafetyValidator,
             IQueryExecutionService queryExecutionService,
             IQuestionSafetyValidator questionSafetyValidator)
        {
            _databaseSchemaService =
                databaseSchemaService;

            _aiSqlGeneratorService =
                aiSqlGeneratorService;

            _sqlSafetyValidator =
                sqlSafetyValidator;

            _queryExecutionService =
                queryExecutionService;

            _questionSafetyValidator =
                questionSafetyValidator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            QueryAssistantViewModel model = new();

            try
            {
                model.AvailableTables =
                    await _databaseSchemaService
                        .GetTableNamesAsync();
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "Database connection failed: "
                    + ex.Message;
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
    QueryAssistantViewModel model)
        {
            try
            {
                // Load all table names for the right-side list
                model.AvailableTables =
                    await _databaseSchemaService
                        .GetTableNamesAsync();

                // Validate the question field
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Block dangerous user requests
                if (!_questionSafetyValidator.IsSafe(
                    model.Question,
                    out string questionSafetyMessage))
                {
                    model.IsSqlSafe = false;

                    model.SqlValidationMessage =
                        questionSafetyMessage;

                    model.GeneratedSql =
                        string.Empty;

                    return View(model);
                }

                // User must select at least one table
                if (model.SelectedTables == null ||
                    model.SelectedTables.Count == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.SelectedTables),
                        "Select at least one table.");

                    return View(model);
                }

                // Prevent sending too many tables to Gemini
                if (model.SelectedTables.Count > 10)
                {
                    ModelState.AddModelError(
                        nameof(model.SelectedTables),
                        "Select a maximum of 10 tables.");

                    return View(model);
                }

                // Ensure posted table names exist in the database
                bool containsInvalidTable =
                    model.SelectedTables.Any(
                        selectedTable =>
                            !model.AvailableTables.Contains(
                                selectedTable,
                                StringComparer.OrdinalIgnoreCase));

                if (containsInvalidTable)
                {
                    ModelState.AddModelError(
                        nameof(model.SelectedTables),
                        "One or more selected tables are invalid.");

                    return View(model);
                }

                // Load schema for only the selected tables
                model.DatabaseSchema =
                    await _databaseSchemaService
                        .GetSelectedTablesSchemaAsync(
                            model.SelectedTables);

                // Send the question and selected schema to Gemini
                model.GeneratedSql =
                    await _aiSqlGeneratorService
                        .GenerateSqlAsync(
                            model.Question,
                            model.DatabaseSchema);

                // Validate Gemini's generated SQL
                SqlValidationResult validationResult =
                    _sqlSafetyValidator.Validate(
                        model.GeneratedSql);

                model.IsSqlSafe =
                    validationResult.IsSafe;

                model.SqlValidationMessage =
                    validationResult.Message;

                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "An error occurred: "
                    + ex.Message;

                return View(model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute(
    QueryAssistantViewModel model)
        {
            try
            {
                model.AvailableTables =
                    await _databaseSchemaService
                        .GetTableNamesAsync();

                SqlValidationResult validationResult =
                    _sqlSafetyValidator.Validate(
                        model.GeneratedSql);

                model.IsSqlSafe =
                    validationResult.IsSafe;

                model.SqlValidationMessage =
                    validationResult.Message;

                if (!validationResult.IsSafe)
                {
                    return View("Index", model);
                }

                QueryExecutionResult result =
                    await _queryExecutionService
                        .ExecuteAsync(model.GeneratedSql);

                model.ResultColumns =
                    result.Columns;

                model.ResultRows =
                    result.Rows;

                model.ExecutionTimeMs =
                    result.ExecutionTimeMs;

                return View("Index", model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "Query execution failed: "
                    + ex.Message;

                return View("Index", model);
            }
        }
    }
}