﻿using System;

namespace Gribble.TransactSql
{
    public static class DataTypes
    {
        public const string SqlBigInt = "bigint";
        public const string SqlBinary = "binary";
        public const string SqlBit = "bit";
        public const string SqlChar = "char";
        public const string SqlDate = "date";
        public const string SqlDateTime = "datetime";
        public const string SqlDateTime2 = "datetime2";
        public const string SqlDateTimeOffset = "datetimeoffset";
        public const string SqlDecimal = "decimal";
        public const string SqlFloat = "float";
        public const string SqlInt = "int";
        public const string SqlMoney = "money";
        public const string SqlNChar = "nchar";
        public const string SqlNumeric = "numeric";
        public const string SqlNVarChar = "nvarchar";
        public const string SqlReal = "real";
        public const string SqlRowVersion = "rowversion";
        public const string SqlSmallDateTime = "smalldatetime";
        public const string SqlSmallInt = "smallint";
        public const string SqlSmallMoney = "smallmoney";
        public const string SqlTime = "time";
        public const string SqlTinyInt = "tinyint";
        public const string SqlUniqueidentifier = "uniqueidentifier";
        public const string SqlVarBinary = "varbinary";
        public const string SqlVarChar = "varchar";
        public const string SqlVariant = "sql_variant";

        public enum SqlTypeId
        {
            Image = 34,
            Text = 35,
            Uniqueidentifier = 36,
            Date = 40,
            Time = 41,
            DateTime2 = 42,
            DateTimeOffset = 43,
            TinyInt = 48,
            SmallInt = 52,
            Int = 56,
            SmallDateTime = 58,
            Real = 59,
            Money = 60,
            DateTime = 61,
            Float = 62,
            Variant = 98,
            NText = 99,
            Bit = 104,
            Decimal = 106,
            Numeric = 108,
            SmallMoney = 122,
            BigInt = 127,
            HierarchyId = 240,
            Geometry = 240,
            Geography = 240,
            VarBinary = 165,
            VarChar = 167,
            Binary = 173,
            Char = 175,
            Timestamp = 189,
            NVarChar = 231,
            NChar = 239,
            Xml = 241,
            SysName = 231
        }

        public static string GetSqlType(Type type, int length)
        {
            if (type == typeof(string) && length <= 0) return SqlNVarChar + " (MAX)";
            if (type == typeof(string) && length > 0) return string.Format(SqlNVarChar + " ({0}) ", length);
            return GetSqlType(type);
        }

        public static string GetSqlType(Type type)
        {
            if (type == typeof(DateTime?) || type == typeof(DateTime)) return SqlDateTime;
            if (type == typeof(DateTimeOffset?) || type == typeof(DateTimeOffset)) return SqlDateTimeOffset;
            if (type == typeof(TimeSpan?) || type == typeof(TimeSpan)) return SqlTime;
            if (type == typeof(Boolean?) || type == typeof(Boolean)) return SqlBit;
            if (type == typeof(Byte[])) return SqlVarBinary;
            if (type == typeof(Byte?) || type == typeof(Byte)) return SqlTinyInt;
            if (type == typeof(Int16?) || type == typeof(Int16)) return SqlSmallInt;
            if (type == typeof(Int32?) || type == typeof(Int32)) return SqlInt;
            if (type == typeof(Int64?) || type == typeof(Int64)) return SqlBigInt;
            if (type == typeof(Decimal?) || type == typeof(Decimal)) return SqlDecimal;
            if (type == typeof(Single?) || type == typeof(Single)) return SqlReal;
            if (type == typeof(Double?) || type == typeof(Double)) return SqlFloat;
            if (type == typeof(Char)) return SqlNChar;
            if (type == typeof(String)) return SqlNVarChar;
            if (type == typeof(Object)) return SqlVariant;
            if (type == typeof(Guid?) || type == typeof(Guid)) return SqlUniqueidentifier;
            throw new Exception(string.Format("No SQL data type found to match CLR data type'{0}'.", type.Name));
        }

        public static Type GetClrType(int sqlType) { return GetClrType(sqlType, false); }

        public static Type GetClrType(int sqlType, bool nullable)
        { return GetClrType((SqlTypeId) sqlType, nullable); }

        public static Type GetClrType(SqlTypeId sqlType) { return GetClrType(sqlType, false); }

        public static Type GetClrType(SqlTypeId sqlType, bool nullable)
        {
            switch (sqlType)
            {
                case SqlTypeId.BigInt: return nullable ? typeof (Int64?) : typeof (Int64);
                case SqlTypeId.Binary: return typeof (Byte[]);
                case SqlTypeId.Bit: return nullable ? typeof (Boolean?) : typeof (Boolean);
                case SqlTypeId.Char: return typeof (Char);
                case SqlTypeId.Date: return nullable ? typeof (DateTime?) : typeof (DateTime);
                case SqlTypeId.DateTime: return nullable ? typeof (DateTime?) : typeof (DateTime);
                case SqlTypeId.DateTime2: return nullable ? typeof (DateTime?) : typeof (DateTime);
                case SqlTypeId.SmallDateTime: return nullable ? typeof (DateTime?) : typeof (DateTime);
                case SqlTypeId.DateTimeOffset:  return nullable ? typeof (DateTimeOffset?) : typeof (DateTimeOffset);
                case SqlTypeId.Decimal: return nullable ? typeof (Decimal?) : typeof (Decimal);
                case SqlTypeId.Float: return nullable ? typeof (Double?) : typeof (Double);
                case SqlTypeId.Int: return nullable ? typeof (Int32?) : typeof (Int32);
                case SqlTypeId.Money: return nullable ? typeof (Decimal?) : typeof (Decimal);
                case SqlTypeId.NChar: return typeof (String);
                case SqlTypeId.Numeric: return nullable ? typeof (Decimal?) : typeof (Decimal);
                case SqlTypeId.VarChar: return typeof (String);
                case SqlTypeId.NVarChar: return typeof (String);
                case SqlTypeId.Real: return nullable ? typeof (Single?) : typeof (Single);
                case SqlTypeId.SmallInt: return nullable ? typeof (Int16?) : typeof (Int16);
                case SqlTypeId.SmallMoney: return nullable ? typeof (Decimal?) : typeof (Decimal);
                case SqlTypeId.Variant: return typeof (Object);
                case SqlTypeId.Time: return nullable ? typeof (TimeSpan?) : typeof (TimeSpan);
                case SqlTypeId.TinyInt: return nullable ? typeof (Byte?) : typeof (Byte);
                case SqlTypeId.Uniqueidentifier: return nullable ? typeof (Guid?) : typeof (Guid);
                case SqlTypeId.VarBinary: return typeof (Byte[]);
                default: throw new Exception(string.Format("No CLR data type found to match SQL data type'{0}'.", sqlType));
            }
        }

        public static Type GetClrType(string sqlType) { return GetClrType(sqlType, false); }

        public static Type GetClrType(string sqlType, bool nullable)
        {
            if (sqlType.Equals(SqlBigInt, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.BigInt, nullable);
            if (sqlType.Equals(SqlBinary, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Binary, nullable);
            if (sqlType.Equals(SqlBit, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Bit, nullable);
            if (sqlType.Equals(SqlChar, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Char, nullable);
            if (sqlType.Equals(SqlDate, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Date, nullable);
            if (sqlType.Equals(SqlDateTime, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.DateTime, nullable);
            if (sqlType.Equals(SqlDateTime2, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.DateTime2, nullable);
            if (sqlType.Equals(SqlSmallDateTime, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.SmallDateTime, nullable);
            if (sqlType.Equals(SqlDateTimeOffset, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.DateTimeOffset, nullable);
            if (sqlType.Equals(SqlDecimal, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Decimal, nullable);
            if (sqlType.Equals(SqlFloat, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Float, nullable);
            if (sqlType.Equals(SqlInt, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Int, nullable);
            if (sqlType.Equals(SqlMoney, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Money, nullable);
            if (sqlType.Equals(SqlNChar, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.NChar, nullable);
            if (sqlType.Equals(SqlNumeric, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Numeric, nullable);
            if (sqlType.Equals(SqlVarChar, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.VarChar, nullable);
            if (sqlType.Equals(SqlNVarChar, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.NVarChar, nullable);
            if (sqlType.Equals(SqlReal, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Real, nullable);
            if (sqlType.Equals(SqlSmallInt, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.SmallInt, nullable);
            if (sqlType.Equals(SqlSmallMoney, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.SmallMoney, nullable);
            if (sqlType.Equals(SqlVariant, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Variant, nullable);
            if (sqlType.Equals(SqlTime, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Time, nullable);
            if (sqlType.Equals(SqlTinyInt, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.TinyInt, nullable);
            if (sqlType.Equals(SqlUniqueidentifier, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.Uniqueidentifier, nullable);
            if (sqlType.Equals(SqlVarBinary, StringComparison.OrdinalIgnoreCase)) return GetClrType(SqlTypeId.VarBinary, nullable);
            throw new Exception(string.Format("No CLR data type found to match SQL data type'{0}'.", sqlType));
        }
    }
}
