using MicroBatchFramework;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            [Command("create_table", "execute query divider to create table sql.")]
            public void CreateTable(
                [Option("-i", "single sql file which contains multiple create table queries.")]string inputSql,
                [Option("-o", "directory path to output sql files.")]string outputPath,
                [Option("-c", "clean output directory before output.")]bool clean = false,
                [Option("-d", "dry-run or not.")]bool dry = true)
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

                var tableBeginKeyword = "CREATE TABLE";
                var replaces = new[] { "CREATE TABLE", "IF NOT EXISTS" };

                // analyze
                var lines = File.ReadAllLines(inputSql, new UTF8Encoding(false));
                var numLines = lines.Select(x => x.TrimEnd())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Where(x => !x.StartsWith("-- ----"))
                    .Where(x => !x.StartsWith("SET FOREIGN_KEY_CHECKS"))
                    .Where(x => !x.StartsWith("DROP SCHEMA"))
                    .Where(x => !x.StartsWith("CREATE SCHEMA"))
                    .Select((x, i) => (index: i, content: x))
                    .ToArray();
                var queryRanges = numLines
                    .Where(x => x.content.StartsWith(tableBeginKeyword))
                    .Zip(numLines.Where(x => x.content.EndsWith(";")), (title, end) => (title, end))
                    .ToArray();
                var queryPerTables = Enumerable.Range(0, queryRanges.Length)
                    .Select(x => numLines
                        .Skip(queryRanges[x].title.index) // CREATE TABLE ....
                        .Take(queryRanges[x].end.index - queryRanges[x].title.index + 1)) // .... ;
                    .Select(x => (title: ExtractTitle(x.Select(y => y.content), tableBeginKeyword, replaces), query: x.Select(y => y.content).ToJoinedString("\n")))
                    .ToArray();

                // output
                if (dry)
                {
                    Context.Logger.LogInformation($"* begin stdout sql.");
                    foreach (var query in queryPerTables)
                    {
                        Context.Logger.LogInformation($"* generating sql {query.title}.");
                        Context.Logger.LogInformation($"------------------------");
                        Context.Logger.LogInformation(query.query);
                    }
                    Context.Logger.LogInformation("pass `-d false` arguments to execute change.");
                }
                else
                {
                    Context.Logger.LogInformation($"* beging generate sql files to the {outputPath}.");
                    Prepare(outputPath, clean);
                    foreach (var query in queryPerTables)
                    {
                        var fileName = $"{query.title}.sql";
                        Context.Logger.LogInformation($"* generating file {fileName}.");
                        Context.Logger.LogInformation($"------------------------");
                        Save(outputPath, fileName, query.query);
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

        static string ExtractTitle(IEnumerable<string> queryLines, string keyword, string[] replaces)
        {
            var target = queryLines.First(y => y.StartsWith(keyword));
            foreach (var replace in replaces)
            {
                target = target.Replace(replace, "");
            }

            return target.Replace("`", "")
                .Replace("(", "")
                .Trim();
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
            File.WriteAllText(path, query, new UTF8Encoding(false));
        }
    }

    public static class StringExtensions
    {
        public static string ToJoinedString(this IEnumerable<string> values, string separator = "")
            => string.Join(separator, values);
    }
}
