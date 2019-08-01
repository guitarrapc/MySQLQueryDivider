using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MySQLQueryDivider.Tests
{
    public class AnalyzerTest_Create
    {
        private static readonly Regex regex = new Regex(@"\s*CREATE\s*TABLE\s*(IF NOT EXISTS)?\s*((?<schema>`?.+`?)\.(?<table>`?.*`?)|(?<table2>`?.+`?))\s*(\(|like)", RegexOptions.IgnoreCase);
        private static readonly string[] escapes = new[] { "-- ----", "--", "SET FOREIGN_KEY_CHECKS", "DROP SCHEMA", "CREATE SCHEMA" };

        [Theory]
        [MemberData(nameof(FromStringTest))]
        public void FromStringUnitTest(FromStringData data)
        {
            var tables = Analyzer.FromString(data.InputSql, regex);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(FromFileTest))]
        public void FromFileUnitTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,                
            };
            var tables = Analyzer.FromFile(data.InputPath, regex, option);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(FromFileMultipleQueryTest))]
        public void FromFileMultipleQueryUnitTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
            };
            var tables = Analyzer.FromFile(data.InputPath, regex, option);
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(FromDirectoryTest))]
        public void FromDirectoryUnitTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
            };
            var files = Analyzer.FromDirectory(data.InputPath, regex, option);
            var tables = files.SelectMany(x => x).ToArray();
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(ComplexSchemaTest))]
        public void RemoveSchemaNameTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
                RemoveSchemaName = true,
            };
            var files = Analyzer.FromFile(data.InputPath, regex, option);
            var tables = files.Select(x => x).ToArray();
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }
        [Theory]
        [MemberData(nameof(ComplexTableTest))]
        public void RemoveSchemaNameNotEffectToTableOnlyTest(FromFileData data)
        {
            var option = new AnalyzerOption
            {
                EscapeLines = escapes,
                RemoveSchemaName = true,
            };
            var files = Analyzer.FromFile(data.InputPath, regex, option);
            var tables = files.Select(x => x).ToArray();
            for (var i = 0; i < tables.Length; i++)
            {
                tables[i].Query.Should().Be(data.Expected[i].Query);
                tables[i].Title.Should().Be(data.Expected[i].Title);
            }
        }


        public class FromStringData
        {
            public string InputSql { get; set; }
            public ParseQuery[] Expected { get; set; }
        }
        public static IEnumerable<object[]> FromStringTest()
        {
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = "CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "genvalue1",
                            Query = "CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));",
                        },
                    },
                 }
            };
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = @"CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));
create table child_table(id int unsigned auto_increment primary key, id_parent int references parent_table(id) match full on update cascade on delete set null) engine=InnoDB;"
.Replace("\r\n", "\n"),
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "genvalue1",
                            Query = "CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));",
                        },
                        new ParseQuery
                        {
                            Title = "child_table",
                            Query = "create table child_table(id int unsigned auto_increment primary key, id_parent int references parent_table(id) match full on update cascade on delete set null) engine=InnoDB;",
                        },
                    }
                 }
            };
            yield return new object[]
            {
                new FromStringData
                {
                    InputSql = @"CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));"
                        + "create table child_table(id int unsigned auto_increment primary key, id_parent int references parent_table(id) match full on update cascade on delete set null) engine=InnoDB;",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "genvalue1",
                            Query = "CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));",
                        },
                        new ParseQuery
                        {
                            Title = "child_table",
                            Query = "create table child_table(id int unsigned auto_increment primary key, id_parent int references parent_table(id) match full on update cascade on delete set null) engine=InnoDB;",
                        },
                    }
                 }
            };
        }

        public class FromFileData
        {
            public string InputPath { get; set; }
            public ParseQuery[] Expected { get; set; }
        }
        public static IEnumerable<object[]> FromFileTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE `Samples` (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_schema.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "TestSchema.Samples",
                            Query = @"CREATE TABLE `TestSchema`.`Samples` (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_backqless.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_schema_backqless.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "TestSchema.Samples",
                            Query = @"CREATE TABLE TestSchema.Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
        }
        public static IEnumerable<object[]> FromFileMultipleQueryTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_tables.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "new_t",
                            Query = @"create table new_t  (like t1);"
                        },
                        new ParseQuery
                        {
                            Title = "log_table",
                            Query = @"create table log_table(row varchar(512));"
                        },
                        new ParseQuery
                        {
                            Title = "ships",
                            Query = @"create table ships(name varchar(255), class_id int, id int);"
                        },
                        new ParseQuery
                        {
                            Title = "ships_guns",
                            Query = @"create table ships_guns(guns_id int, ship_id int);"
                        },
                        new ParseQuery
                        {
                            Title = "guns",
                            Query = @"create table guns(id int, power decimal(7,2), callibr decimal(10,3));"
                        },
                        new ParseQuery
                        {
                            Title = "ship_class",
                            Query = @"create table ship_class(id int, class_name varchar(100), tonange decimal(10,2), max_length decimal(10,2), start_build year, end_build year(4), max_guns_size int);"
                        },
                        new ParseQuery
                        {
                            Title = "some_table_",
                            Query = @"create table `some table $$`(id int auto_increment key, class varchar(10), data binary) engine=MYISAM;"
                        },
                        new ParseQuery
                        {
                            Title = "quengine",
                            Query = @"create table quengine(id int auto_increment key, class varchar(10), data binary) engine='InnoDB';"
                        },
                        new ParseQuery
                        {
                            Title = "quengine",
                            Query = @"create table quengine(id int auto_increment key, class varchar(10), data binary) engine=""Memory"";"
                        },
                        new ParseQuery
                        {
                            Title = "quengine",
                            Query = @"create table quengine(id int auto_increment key, class varchar(10), data binary) engine=`CSV`;"
                        },
                        new ParseQuery
                        {
                            Title = "quengine",
                            Query = @"create table quengine(id int auto_increment key, class varchar(10), data binary COMMENT 'CSV') engine=MyISAM;"
                        },
                        new ParseQuery
                        {
                            Title = "quengine",
                            Query = @"create table quengine(id int auto_increment key, class varchar(10), data binary) engine=Aria;"
                        },
                        new ParseQuery
                        {
                            Title = "parent_table",
                            Query = @"create table `parent_table`(id int primary key, column1 varchar(30), index parent_table_i1(column1(20)), check(char_length(column1)>10)) engine InnoDB;"
                        },
                        new ParseQuery
                        {
                            Title = "child_table",
                            Query = @"create table child_table(id int unsigned auto_increment primary key, id_parent int references parent_table(id) match full on update cascade on delete set null) engine=InnoDB;"
                        },
                        new ParseQuery
                        {
                            Title = "another_some_table_",
                            Query = @"create table `another some table $$` like `some table $$`;"
                        },
                        new ParseQuery
                        {
                            Title = "actor",
                            Query = @"create table `actor` (`last_update` timestamp default CURRENT_TIMESTAMP, `birthday` datetime default CURRENT_TIMESTAMP ON UPDATE LOCALTIMESTAMP);"
                        },
                        new ParseQuery
                        {
                            Title = "boolean_table",
                            Query = @"create table boolean_table(c1 bool, c2 boolean default true);"
                        },
                        new ParseQuery
                        {
                            Title = "default_table",
                            Query = @"create table default_table(c1 int default 42, c2 int default -42, c3 varchar(256) DEFAULT _utf8mb3'xxx');"
                        },
                        new ParseQuery
                        {
                            Title = "ts_table",
                            Query = @"create table ts_table(
  ts1 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  ts2 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE LOCALTIME,
  ts3 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE LOCALTIMESTAMP,
  ts4 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP(),
  ts5 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE LOCALTIME(),
  ts6 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE LOCALTIMESTAMP(),
  ts7 TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE NOW(),
  ts8 TIMESTAMP(6) NOT NULL,
  ts9 TIMESTAMP(6) NOT NULL DEFAULT NOW(6) ON UPDATE NOW(6)
);".Replace("\r\n", "\n")
                        },
                        new ParseQuery
                        {
                            Title = "with_check",
                            Query = @"create table with_check (c1 integer not null,c2 varchar(22),constraint c1 check (c2 in ('a', 'b', 'c')));"
                        },
                        new ParseQuery
                        {
                            Title = "genvalue1",
                            Query = @"CREATE TABLE genvalue1 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) STORED, PRIMARY KEY (id));"
                        },
                        new ParseQuery
                        {
                            Title = "genvalue2",
                            Query = @"CREATE TABLE genvalue2 (id binary(16) NOT NULL, val char(32) AS (hex(id)) STORED, PRIMARY KEY (id));"
                        },
                        new ParseQuery
                        {
                            Title = "genvalue3",
                            Query = @"CREATE TABLE genvalue3 (id binary(16) NOT NULL, val char(32) GENERATED ALWAYS AS (hex(id)) VIRTUAL, PRIMARY KEY (id));"
                        },
                        new ParseQuery
                        {
                            Title = "cast_charset",
                            Query = @"CREATE TABLE cast_charset (col BINARY(16) GENERATED ALWAYS AS (CAST('xx' as CHAR(16) CHARACTER SET BINARY)) VIRTUAL);"
                        },
                        new ParseQuery
                        {
                            Title = "cast_charset",
                            Query = @"CREATE TABLE cast_charset (col BINARY(16) GENERATED ALWAYS AS (CAST('xx' as CHAR(16) CHARSET BINARY)) VIRTUAL);"
                        },
                        new ParseQuery
                        {
                            Title = "check_table_kw",
                            Query = @"CREATE TABLE check_table_kw (id int primary key, upgrade varchar(256), quick varchar(256), fast varchar(256), medium varchar(256), extended varchar(256), changed varchar(256));"
                        },
                        new ParseQuery
                        {
                            Title = "sercol1",
                            Query = @"CREATE TABLE sercol1 (id SERIAL, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "sercol2",
                            Query = @"CREATE TABLE sercol2 (id SERIAL PRIMARY KEY, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "sercol3",
                            Query = @"CREATE TABLE sercol3 (id SERIAL NULL, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "sercol4",
                            Query = @"CREATE TABLE sercol4 (id SERIAL NOT NULL, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "serval1",
                            Query = @"CREATE TABLE serval1 (id SMALLINT SERIAL DEFAULT VALUE, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "serval2",
                            Query = @"CREATE TABLE serval2 (id SMALLINT SERIAL DEFAULT VALUE PRIMARY KEY, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "serval3",
                            Query = @"CREATE TABLE serval3 (id SMALLINT(3) NULL SERIAL DEFAULT VALUE, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "serval4",
                            Query = @"CREATE TABLE serval4 (id SMALLINT(5) UNSIGNED SERIAL DEFAULT VALUE NOT NULL, val INT);"
                        },
                        new ParseQuery
                        {
                            Title = "serial",
                            Query = @"CREATE TABLE serial (serial INT);"
                        },
                        new ParseQuery
                        {
                            Title = "float_table",
                            Query = @"CREATE TABLE float_table (f1 FLOAT, f2 FLOAT(10), f3 FLOAT(7,4));"
                        },
                        new ParseQuery
                        {
                            Title = "USER",
                            Query = @"CREATE TABLE USER (INTERNAL BOOLEAN DEFAULT FALSE);"
                        },
                    },
                }
            };
        }

        public static IEnumerable<object[]> FromDirectoryTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/sql/",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                        new ParseQuery
                        {
                            Title = "TestSchema.Samples",
                            Query = @"CREATE TABLE TestSchema.Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                },
            };
        }

        public static IEnumerable<object[]> ComplexSchemaTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_schema.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE `Samples` (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_schema_backqless.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
        }
        public static IEnumerable<object[]> ComplexTableTest()
        {
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE `Samples` (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
            yield return new object[]
            {
                new FromFileData
                {
                    InputPath = "test_data/create_table_complex_backqless.sql",
                    Expected = new [] {
                        new ParseQuery
                        {
                            Title = "Samples",
                            Query = @"CREATE TABLE Samples (
    `Id` BIGINT(20) NOT NULL AUTO_INCREMENT,
    `SampleId` INT(11) NOT NULL,
    `MasterId` INT(11) NOT NULL,
    `Value` INT(11) NOT NULL DEFAULT '0',
    `Status` TINYINT(3) UNSIGNED NOT NULL DEFAULT '1',
    `Created` DATETIME(6) NOT NULL,
    PRIMARY KEY(`Id`),
    UNIQUE INDEX `UQ_SampleId_MasterId` (`SampleId`, `MasterId`),
    INDEX `SampleId_Status` (`SampleId`, `Status`),
    INDEX `MasterId_Status` (`MasterId`, `Status`)
)
COLLATE = 'utf8mb4_general_ci'
ENGINE = InnoDB
AUTO_INCREMENT = 9
;".Replace("\r\n", "\n"),
                        },
                    },
                }
            };
        }
    }
}
