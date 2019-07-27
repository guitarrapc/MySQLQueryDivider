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
            private static string[] escapes = new[] { "-- ----", "--", "SET FOREIGN_KEY_CHECKS", "DROP SCHEMA", "CREATE SCHEMA" };

            [Command("create_table", "execute query divider to create table sql.")]
            public void CreateTable(
                [Option("-i", "single sql file which contains multiple create table queries.")]string inputSql,
                [Option("-o", "directory path to output sql files.")]string outputPath,
                [Option("-r", "regex pattern to match filename from query.")]string titleRegex = @"\s*CREATE\s*TABLE\s*(IF NOT EXISTS)?\s*(?<schema>`?.+`?)\.?(?<table>`?.*`?)",
                [Option("-c", "clean output directory before output.")]bool clean = false,
                [Option("-d", "dry-run or not.")]bool dry = true
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

                if (!File.Exists(inputSql)) throw new FileNotFoundException($"specified file not found. {inputSql}");

                // analyze
                var regex = new Regex(@"\s*CREATE\s*TABLE\s*(IF NOT EXISTS)?\s*(?<schema>`?.+`?)\.?(?<table>`?.*`?)", RegexOptions.IgnoreCase);
                var queryPerTables = AnalyzeQuery.FromFile(inputSql, escapes, regex);

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
                Context.Logger.LogInformation($"Usage: {nameof(MySQLQueryDivider)} [-i input_sql.sql] [-o output_directory_path] [-c true|false] [-d true|false] [-version] [-help]");
                Context.Logger.LogInformation($@"E.g., run this: {nameof(MySQLQueryDivider)} create_table -i input_sql.sql -o ./sql -clean false -dry true");
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
