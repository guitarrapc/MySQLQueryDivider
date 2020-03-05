## MySQLQueryDivider

[![CircleCI](https://circleci.com/gh/guitarrapc/MySQLQueryDivider.svg?style=svg)](https://circleci.com/gh/guitarrapc/MySQLQueryDivider) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![codecov](https://codecov.io/gh/guitarrapc/MySQLQueryDivider/branch/master/graph/badge.svg)](https://codecov.io/gh/guitarrapc/MySQLQueryDivider) [![NuGet](https://img.shields.io/nuget/v/mysqlquerydivider.svg)](https://www.nuget.org/packages/mysqlquerydivider)

## Install

install .net global tool.

```shell
dotnet tool install --global mysqlquerydivider
```

## How to use

divide sql query.

```shell
mysqlquerydivider from_string -i "CREATE TABLE create table new_t  (like t1);create table log_table(row varchar(512));" -o ./bin/out
```

divide file.

```shell
mysqlquerydivider from_file -i ./input.sql -o ./bin/out
```

divide folder.

```shell
mysqlquerydivider from_dir -i ./input/sql -o ./bin/out
```