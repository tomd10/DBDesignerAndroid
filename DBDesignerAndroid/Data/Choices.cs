namespace DBDesignerWIP
{
    public static class Choices
    {
        public static readonly List<string> charsets = new List<string>() { "utf8mb4", "utf8mb3", "latin2" };
        public static readonly List<string> collates = new List<string>() { "_general_ci", "_general_cs" };
        public static readonly List<string> integerColumn = new List<string>() { "INT", "TINYINT", "SMALLINT", "MEDIUMINT",  "BIGINT" };
        public static readonly List<string> enumColumn = new List<string>() { "ENUM", "SET" };
        public static readonly List<string> textColumn = new List<string>() { "VARCHAR", "CHAR", "TEXT", "TINYTEXT", "MEDIUMTEXT", "LONGTEXT"  };
        public static readonly List<string> binaryColumn = new List<string>() { "VARBINARY", "BINARY", "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB" };
        public static readonly List<string> decimalColumn = new List<string>() { "FLOAT", "DOUBLE", "DECIMAL" };
        public static readonly List<string> datetimeColumn = new List<string>() { "DATETIME", "DATE", "TIME", "TIMESTAMP", "YEAR" };
        public static readonly List<string> onUpdateDelete = new List<string>() { "", "RESTRICT", "CASCADE", "SET NULL", "NO ACTION", "SET DEFAULT" };
        public static readonly List<string> engines = new List<string>() { "InnoDB", "Aria", "MEMORY" };
        public static readonly List<string> keywords = new List<string>() {"CREATE", "TABLE", "DATABASE", "IF", "NOT", "EXISTS", "DEFAULT", "NULL", 
            "ENGINE", "AUTO_INCREMENT", "DROP", "ALTER", "CHARACTER", "SET", "COLLATE", "COLUMN", "KEY", "CONSTRAINT", "PRIMARY", "UNIQUE", "FOREIGN", "UNSIGNED",
        "ZEROFILL", "ADD", "RENAME", "CHANGE", "REFERENCES", "DATE", "INT", "SMALLINT", "MEDIUMINT", "TINYINT", "COMMENT", "BIGINT", "CHAR", "VARCHAR",
        "TEXT", "ENUM"};


        public static List<string> dbNames = new List<string>();
        public static List<string> tableNames = new List<string>();

        public static void SetDbNames()
        {
            dbNames = new List<string>();
            foreach (Database database in DataStore.databases)
            {
                dbNames.Add(database.name);
            }
        }

        public static void SetTableNames()
        {
            tableNames = new List<string>();
            foreach (Table t in DataStore.activeDatabase.tables)
            {
                tableNames.Add(t.name);
            }
        }

        public enum ColumnTypes
        {
            Text, Integer, Decimal, Enum, Binary, DateTime
        }

        public static List<string> GetTypeNames(ColumnTypes ct)
        {
            if (ct == ColumnTypes.Text) return textColumn;
            if (ct == ColumnTypes.Binary) return binaryColumn;
            if (ct == ColumnTypes.Integer) return integerColumn;
            if (ct == ColumnTypes.Decimal) return decimalColumn;
            if (ct == ColumnTypes.Enum) return enumColumn;
            else return datetimeColumn;
        }

        public static List<string> GetTableNames()
        {
            List<string> names = new List<string>();
            if (DataStore.activeDatabase != null)
            {
                foreach (Table t in DataStore.activeDatabase.tables)
                {
                    names.Add(t.name);
                }
            }

            return names;
        }

        public enum ConstraintTypes
        {
            Key, PrimaryKey, UniqueKey, ForeignKey
        }

    }
}
