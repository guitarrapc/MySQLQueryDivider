using MicroBatchFramework;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MySQLQueryDivider
{
    class Program
    {
        static async Task Main(string[] args)
            => await BatchHost.CreateDefaultBuilder()
                .RunBatchEngineAsync<QueryDivider>(args);

        public class QueryDivider : BatchBase
        {
            // want to cover `schema`.`table`, schema.table, `table` and table in single regular expression.
            const string TITLE_REGEX = @"\s*CREATE\s*TABLE\s+(IF NOT EXISTS\s+)?(?<schema>`?.*`?\.)?(?<table>`?.*`?)\s*(\(|like)";
            static readonly string[] escapeLines = new[] { "-- ----", "--", "SET FOREIGN_KEY_CHECKS", "DROP SCHEMA", "CREATE SCHEMA" };
            static readonly Encoding encode = new UTF8Encoding(false);

            /// <summary>
            /// mysqlquerydivider from_string -i "CREATE TABLE create table new_t  (like t1);create table log_table(row varchar(512));" -o ./sql
            /// </summary>
            /// <param name="input"></param>
            /// <param name="output"></param>
            /// <param name="titleRegex"></param>
            /// <param name="clean"></param>
            /// <param name="dry"></param>
            [Command("from_string", "execute query divider to create table sql.")]
            public void FromString(
                [Option("-i", "sql query which contains multiple create table queries. must end with ';' for each query.")]string input,
                [Option("-o", "directory path to output sql files.")]string output,
                [Option("-r", "regex pattern to match filename from query.")]string titleRegex = TITLE_REGEX,
                [Option("--removeschemaname", "remove schema name from query and filename.")]bool removeSchemaName = false,
                [Option("--clean", "clean output directory before output.")]bool clean = false,
                [Option("--dry", "dry-run or not.")]bool dry = true
            )
            {
                if (dry)
                {
                    Context.Logger.LogInformation("dry-run, nothing will change.");
                }
                else
                {
                    Context.Logger.LogInformation("running divider.");
                }

                // analyze
                var regex = new Regex(titleRegex, RegexOptions.IgnoreCase);
                var option = new AnalyzerOption
                {
                    EscapeLines = escapeLines,
                    Encode = encode,
                    RemoveSchemaName = removeSchemaName,
                };
                var queryPerTables = Analyzer.FromString(input, regex, option);

                // output
                Output(output, clean, dry, queryPerTables);
            }

            /// <summary>
            /// mysqlquerydivider from_file -i ./input.sql -o ./sql
            /// </summary>
            /// <param name="input"></param>
            /// <param name="output"></param>
            /// <param name="titleRegex"></param>
            /// <param name="clean"></param>
            /// <param name="dry"></param>
            [Command("from_file", "execute query divider to create table sql.")]
            public void FromFile(
                [Option("-i", "single sql file which contains multiple create table queries.")]string input,
                [Option("-o", "directory path to output sql files.")]string output,
                [Option("-r", "regex pattern to match filename from query.")]string titleRegex = TITLE_REGEX,
                [Option("--removeschemaname", "remove schema name from query and filename.")]bool removeSchemaName = false,
                [Option("--clean", "clean output directory before output.")]bool clean = false,
                [Option("--dry", "dry-run or not.")]bool dry = true
            )
            {
                if (dry)
                {
                    Context.Logger.LogInformation("dry-run, nothing will change.");
                }
                else
                {
                    Context.Logger.LogInformation("running divider.");
                }

                if (!File.Exists(input)) throw new FileNotFoundException($"specified file not found. {input}");

                // analyze
                var regex = new Regex(titleRegex, RegexOptions.IgnoreCase);
                var option = new AnalyzerOption
                {
                    EscapeLines = escapeLines,
                    Encode = encode,
                    RemoveSchemaName = removeSchemaName,
                };
                var queryPerTables = Analyzer.FromFile(input, regex, option);

                // output
                Output(output, clean, dry, queryPerTables);
            }

            /// <summary>
            /// mysqlquerydivider from_directory -i ./input/sql -o ./sql
            /// </summary>
            /// <param name="input"></param>
            /// <param name="output"></param>
            /// <param name="titleRegex"></param>
            /// <param name="clean"></param>
            /// <param name="dry"></param>
            [Command("from_dir", "execute query divider to create table sql.")]
            public void FromDirectory(
                [Option("-i", "directory path which contains *.sql files.")]string input,
                [Option("-o", "directory path to output sql files.")]string output,
                [Option("-r", "regex pattern to match filename from query.")]string titleRegex = TITLE_REGEX,
                [Option("--removeschemaname", "remove schema name from query and filename.")]bool removeSchemaName = false,
                [Option("--clean", "clean output directory before output.")]bool clean = false,
                [Option("--dry", "dry-run or not.")]bool dry = true
            )
            {
                if (dry)
                {
                    Context.Logger.LogInformation("dry-run, nothing will change.");
                }
                else
                {
                    Context.Logger.LogInformation("running divider.");
                }

                if (!Directory.Exists(input)) throw new FileNotFoundException($"specified directory not found. {input}");

                // analyze
                var regex = new Regex(titleRegex, RegexOptions.IgnoreCase);
                var option = new AnalyzerOption
                {
                    EscapeLines = escapeLines,
                    Encode = encode,
                    RemoveSchemaName = removeSchemaName,
                };
                var queryPerTables = Analyzer.FromDirectory(input, regex, option);

                // output
                foreach (var queries in queryPerTables)
                {
                    Output(output, clean, dry, queries);
                }
            }
            /// <summary>
            /// Provide unix style command argument: -version --version -v + version command
            /// </summary>
            [Command(new[] { "version", "-v", "-version", "--version" }, "show version")]
            public void Version()
            {
                var version = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion
                    .ToString();
                Context.Logger.LogInformation($"{nameof(MySQLQueryDivider)} v{version}");
            }

            /// <summary>
            /// Provide unix style command argument: -help --help -h + override default help / list
            /// </summary>
            /// <remarks>
            /// Also override default help. no arguments execution will fallback to here.
            /// </remarks>
            [Command(new[] { "help", "list", "-h", "-help", "--help" }, "show help")]
            public void Help()
            {
                Context.Logger.LogInformation($"Usage: {nameof(MySQLQueryDivider)} [from_string|from_file|from_dir] [-i input_sql.sql] [-o output_directory_path] [--removeschemaname true|false] [--clean true|false] [--dry true|false] [-version] [-help]");
                Context.Logger.LogInformation($@"E.g., divide query args: {nameof(MySQLQueryDivider)} from_string -i ""CREATE TABLE create table new_t(like t1); create table log_table(row varchar(512));"" -o ./bin/out");
                Context.Logger.LogInformation($@"E.g., divide sql in file: {nameof(MySQLQueryDivider)} from_file -i input_sql.sql -o ./bin/out");
                Context.Logger.LogInformation($@"E.g., divide sql in folder: {nameof(MySQLQueryDivider)} from_dir -i ./input/sql -o ./bin/out");
            }

            /// <summary>
            /// Output queries to each file.
            /// </summary>
            /// <param name="outputPath"></param>
            /// <param name="clean"></param>
            /// <param name="dry"></param>
            /// <param name="queryPerTables"></param>
            private void Output(string outputPath, bool clean, bool dry, ParseQuery[] queryPerTables)
            {
                // output
                if (dry)
                {
                    Context.Logger.LogInformation($"* begin stdout sql.");
                    foreach (var query in queryPerTables)
                    {
                        Context.Logger.LogInformation($"* generating {query.Title}.sql.");
                        Context.Logger.LogInformation($"{query.Query}");
                    }
                    Context.Logger.LogInformation("pass `-d false` arguments to execute change.");
                }
                else
                {
                    Context.Logger.LogInformation($"* beging generate sql files to the {outputPath}.");
                    Prepare(outputPath, clean);
                    var current = 1;
                    foreach (var query in queryPerTables)
                    {
                        var fileName = $"{query.Title}.sql";
                        Context.Logger.LogInformation($"{current++}/{queryPerTables.Length} {fileName}");
                        Save(outputPath, fileName, query.Query);
                    }
                }
            }
        }

        static void Prepare(string path, bool clean)
        {
            if (clean && Directory.Exists(path))
                Directory.Delete(path, true);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static void Save(string basePath, string title, string query)
        {
            if (query == null) throw new ArgumentNullException($"{title} query {nameof(query)} is null.");
            if (query.Length == 0) throw new ArgumentOutOfRangeException($"{title} query {nameof(query)} is empty.");

            // add empty line at end of file
            if (query.Last() != '\n')
                query += "\n";

            var path = Path.Combine(basePath, title);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, query, new UTF8Encoding(false));
        }
    }
}
