## MySQLQueryDivider

[![CircleCI](https://circleci.com/gh/guitarrapc/MySQLQueryDivider.svg?style=svg)](https://circleci.com/gh/guitarrapc/MySQLQueryDivider)

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