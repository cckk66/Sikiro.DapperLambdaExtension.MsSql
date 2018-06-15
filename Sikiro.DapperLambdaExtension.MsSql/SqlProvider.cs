﻿using System;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using Sikiro.DapperLambdaExtension.MsSql.Helper;
using Sikiro.DapperLambdaExtension.MsSql.Model;

namespace Sikiro.DapperLambdaExtension.MsSql
{
    public class SqlProvider<T>
    {
        internal DataBaseContext<T> Context { get; set; }

        public SqlProvider()
        {
            Params = new DynamicParameters();
        }

        public string SqlString { get; private set; }

        public DynamicParameters Params { get; private set; }

        public SqlProvider<T> FormatGet()
        {
            var selectSql = ResolveExpression.ResolveSelect(typeof(T).GetProperties(), Context.Set.SelectExpression, 1);

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set.OrderbyExpressionList);

            SqlString = $"{selectSql} {fromTableSql} {whereSql} {orderbySql}";

            return this;
        }

        public SqlProvider<T> FormatToList()
        {
            var selectSql = ResolveExpression.ResolveSelect(typeof(T).GetProperties(), Context.Set.SelectExpression, Context.Set.TopNum);

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set.OrderbyExpressionList);

            SqlString = $"{selectSql} {fromTableSql} {whereSql} {orderbySql}";

            return this;
        }

        public SqlProvider<T> FormatToPageList(int pageIndex, int pageSize)
        {
            var orderbySql = ResolveExpression.ResolveOrderBy(Context.Set.OrderbyExpressionList);
            if (string.IsNullOrEmpty(orderbySql))
                throw new Exception("分页查询需要排序条件");

            var selectSql = ResolveExpression.ResolveSelect(typeof(T).GetProperties(), Context.Set.SelectExpression, pageSize);

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            SqlString = $"SELECT COUNT(1) {fromTableSql} {whereSql};";
            SqlString += $@"{selectSql}
            FROM    ( SELECT *
                      ,ROW_NUMBER() OVER ( {orderbySql} ) AS ROWNUMBER
                      {fromTableSql}
                      {whereSql}
                    ) T
            WHERE   ROWNUMBER > {(pageIndex - 1) * pageSize}
                    AND ROWNUMBER <= {pageIndex * pageSize} {orderbySql};";

            return this;
        }

        public SqlProvider<T> FormatCount()
        {
            var selectSql = "SELECT COUNT(1)";

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            SqlString = $"{selectSql} {fromTableSql} {whereSql} ";

            return this;
        }

        public SqlProvider<T> FormatExists()
        {
            var selectSql = "SELECT TOP 1 1";

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            SqlString = $"{selectSql} {fromTableSql} {whereSql}";

            return this;
        }

        public SqlProvider<T> FormatDelete()
        {
            var deleteSql = Context.Set.TopNum.HasValue ? $"DELETE TOP ({Context.Set.TopNum.Value})" : "DELETE";

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            SqlString = $"{deleteSql} {fromTableSql} {whereSql }";

            return this;
        }

        public SqlProvider<T> FormatInsert(T entity)
        {
            var paramsAndValuesSql = FormatInsertParamsAndValues(entity);

            SqlString = $"INSERT INTO {FormatTableName(false)} {paramsAndValuesSql}";
            return this;
        }

        public SqlProvider<T> FormatUpdate(Expression<Func<T, T>> updateExpression)
        {
            var update = ResolveExpression.ResolveUpdate(updateExpression);

            var where = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = where.SqlCmd;

            Params = where.Param;
            Params.AddDynamicParams(update.Param);

            SqlString = $"UPDATE {FormatTableName(false)} {update.SqlCmd} {whereSql}";

            return this;
        }

        public SqlProvider<T> FormatSum(LambdaExpression lambdaExpression)
        {
            var selectSql = ResolveExpression.ResolveSum(typeof(T).GetProperties(), lambdaExpression);

            var fromTableSql = FormatTableName();

            var whereParams = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = whereParams.SqlCmd;

            Params = whereParams.Param;

            SqlString = $"{selectSql} {fromTableSql} {whereSql} ";

            return this;
        }

        public SqlProvider<T> FormatUpdateSelect(Expression<Func<T, T>> updator)
        {
            var update = ResolveExpression.ResolveUpdate(updator);

            var selectSql = ResolveExpression.ResolveSelectOfUpdate(typeof(T).GetProperties(), Context.Set.SelectExpression);

            var where = ResolveExpression.ResolveWhere(Context.Set.WhereExpression);

            var whereSql = where.SqlCmd;

            Params = where.Param;
            Params.AddDynamicParams(update.Param);

            var topSql = Context.Set.TopNum.HasValue ? " TOP " + Context.Set.TopNum.Value : "";
            SqlString = $"UPDATE {topSql} {FormatTableName(false)} WITH ( UPDLOCK, READPAST ) {update.SqlCmd} {selectSql} {whereSql}";

            return this;
        }

        private string FormatTableName(bool isNeedFrom = true)
        {
            var typeOfTableClass = Context.Set.TableType;

            var tableName = typeOfTableClass.GetTableAttributeName();

            SqlString = isNeedFrom ? $" FROM {tableName} " : $" {tableName} ";

            return SqlString;
        }

        private string FormatInsertParamsAndValues(T entity)
        {
            var paramSqlBuilder = new StringBuilder(100);
            var valueSqlBuilder = new StringBuilder(100);

            var properties = entity.GetProperties();

            var isAppend = false;
            foreach (var propertiy in properties)
            {
                if (isAppend)
                {
                    paramSqlBuilder.Append(",");
                    valueSqlBuilder.Append(",");
                }

                var name = propertiy.GetColumnAttributeName();

                paramSqlBuilder.Append(name);
                valueSqlBuilder.Append("@" + name);

                Params.Add("@" + name, propertiy.GetValue(entity));

                isAppend = true;
            }

            return $"({paramSqlBuilder}) VALUES  ({valueSqlBuilder})";
        }
    }
}