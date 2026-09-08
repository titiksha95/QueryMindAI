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

        public QueryController(
            IDatabaseSchemaService databaseSchemaService,
            IAiSqlGeneratorService aiSqlGeneratorService,
            ISqlSafetyValidator sqlSafetyValidator,
            IQueryExecutionService queryExecutionService)
        {
            _databaseSchemaService =
                databaseSchemaService;

            _aiSqlGeneratorService =
                aiSqlGeneratorService;

            _sqlSafetyValidator =
                sqlSafetyValidator;

            _queryExecutionService =
                queryExecutionService;
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
                model.AvailableTables =
                    await _databaseSchemaService
                        .GetTableNamesAsync();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Temporary SQL for testing the form.
                // AI generation will replace this later.
                model.DatabaseSchema =
                await _databaseSchemaService
                    .GetDatabaseSchemaAsync();

                model.GeneratedSql =
                await _aiSqlGeneratorService
                    .GenerateSqlAsync(
                        model.Question,
                        model.DatabaseSchema);

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
                    "An error occurred: " + ex.Message;

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