namespace DBDesignerWIP
{
    public static class Methods
    {
        public static void LoadDatabases()
        {
            foreach (string database in DataStore.dbConnection.GetDatabases())
            {
                Database db = new Database(DataStore.dbConnection.GetCreateDatabase(database));
                DataStore.databases.Add(db);

                List<string> tables = DataStore.dbConnection.GetTables(database);
                foreach (string table in tables)
                {
                    db.tables.Add(new Table(db, DataStore.dbConnection.GetCreateTable(database, table)));
                }
            }

            foreach (Database db in DataStore.databases)
            {
                db.LoadConstraints();
            }
        }

        public static Database GetNthDatabase(int n)
        {
            if (n < 0 || n >= DataStore.databases.Count)
            {
                return null;
            }
            else return DataStore.databases[n];
        }

        public static bool CreateDatabase(string name, string charset, string collate, out string errorMessage)
        {
            if (!Check.CheckDatabaseName(name, out errorMessage))
            {
                return false;
            }
            else
            {
                Database db = new Database(name, charset, charset + collate, new List<Table>());
                DataStore.databases.Add(db);

                DataStore.batch.Add(db.GetStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateTable(string name, bool isTemporary, string engine, string charset, string collate, int auto_increment, string comment, out string errorMessage)
        {
            if (!Check.CheckTableName(name, out errorMessage))
            {
                return false;
            }
            if (auto_increment < 0)
            {
                errorMessage = "Auto increment can't be negative.";
                return false;
            }
            else
            {
                Table t = new Table(name, isTemporary, engine, charset, collate, auto_increment.ToString(), comment, DataStore.activeDatabase);
                t.CreateDefaultColumn();
                DataStore.activeDatabase.tables.Add(t);

                DataStore.batch.Add(t.GetStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterTable(Table table, string name, int autoIncrement, string charset, string collate, string engine, string comment, out string errorMessage)
        {
            if (table.name != name && !Check.CheckTableName(name, out errorMessage))
            {
                return false;
            }
            if (autoIncrement < 0)
            {
                errorMessage = "Auto increment can't be negative.";
                return false;
            }
            else
            {
                if (name != table.name)
                {
                    DataStore.batch.Add(table.GetAlterName(name));
                    table.name = name;
                }
                if (table.engine != engine || int.Parse(table.auto_increment) != autoIncrement || table.collate != collate || table.charset != charset || table.comment != comment)
                {
                    table.engine = engine;
                    table.auto_increment = autoIncrement.ToString();
                    table.collate = collate;
                    table.charset = charset;
                    table.comment = comment;
                    DataStore.batch.Add(table.GetAlterStatement());
                }


                errorMessage = "";
                return true;
            }
        }

        public static void DropDatabase (int n, out string name)
        {
            Database db = GetNthDatabase(n);
            name = db.name;
            DataStore.batch.Add(db.GetDropStatement());

            if (DataStore.activeTable != null && DataStore.activeTable.parent == db) DataStore.activeTable = null;
            if (DataStore.activeDatabase == db) DataStore.activeDatabase = null;

            DataStore.databases.Remove(db);
        }

        public static bool DropTable(int n, out string name, out string errorMessage)
        {
            Table t = DataStore.activeDatabase.GetNthTable(n);
            List<ConstraintFK> constraints = DataStore.activeDatabase.GetTableFKReference(t);
            name = t.name;
            if (constraints.Count == 0)
            {
                errorMessage = "";
                DataStore.batch.Add(t.GetDropStatement());
                DataStore.activeDatabase.tables.Remove(t);
                return true;
            }
            else
            {
                errorMessage = "Can't drop table " + t.name + ". It's referenced by:\n";
                foreach (ConstraintFK constraintFK in constraints)
                {
                    errorMessage = errorMessage + constraintFK.name + " of table " + constraintFK.parent.name + "\n";
                }
                return false;
            }
        }

        public static bool CreateTextColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string charset, string collate, out string errorMessage)
        {
            if (!Check.CheckTextColumn(name, type, nullAllowed, defaultValue, comment, size, charset, collate, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                TextColumn tc = new TextColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, charset, charset + collate);
                DataStore.activeTable.columns.Add(tc);
                DataStore.batch.Add(tc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterTextColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string charset, string collate, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckTextColumn(name, type, nullAllowed, defaultValue, comment, size, charset, collate, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                TextColumn tc = new TextColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, charset, charset + collate);
                DataStore.batch.Add(col.GetAlterColumnStatement(tc, position));
                col.ReplaceCol(tc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateIntegerColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, bool unsigned, bool zerofill, bool autoIncrement, out string errorMessage)
        {
            if(!Check.CheckIntegerColumn(name, type, nullAllowed, defaultValue, comment, size, unsigned, zerofill, autoIncrement, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                IntegerColumn ic = new IntegerColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, unsigned, zerofill, autoIncrement);
                if (autoIncrement)
                {
                    ic.autoincrement = false;
                    DataStore.activeTable.columns.Add(ic);
                    DataStore.batch.Add(ic.GetAddColumnStatement());
                    ConstraintK key = new ConstraintK(DataStore.activeTable, name + "_KEY_" + GetRandomString(10), new List<Column>() { ic });
                    DataStore.batch.Add(key.GetAddStatement());
                    DataStore.activeTable.constraints.Add(key);
                    ic.autoincrement = true;
                    DataStore.batch.Add("ALTER TABLE `" + ic.parent.parent.name + "`.`" + ic.parent.name + "` CHANGE COLUMN `" + ic.name + "` " + ic.GetStatement() + " ;");
                }
                else
                {
                    DataStore.activeTable.columns.Add(ic);
                    DataStore.batch.Add(ic.GetAddColumnStatement());
                }


                errorMessage = "";
                return true;
            }
        }


        public static bool AlterIntegerColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, bool unsigned, bool zerofill, bool autoIncrement, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (col is IntegerColumn) { (col as IntegerColumn).autoincrement = false; }
            if (!Check.CheckIntegerColumn(name, type, nullAllowed, defaultValue, comment, size, unsigned, zerofill, autoIncrement, out errorMessage))
            {
                return false;
            }
            else
            {

                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                IntegerColumn ic = new IntegerColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, unsigned, zerofill, autoIncrement);
                DataStore.batch.Add(col.GetAlterColumnStatement(ic, position));
                col.ReplaceCol(ic, position);
                errorMessage = "";
                return true;
            }
        }

        public static bool CreateDecimalColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, int d, out string errorMessage)
        {
            if (!Check.CheckDecimalColumn(name, type, nullAllowed, defaultValue, comment, size, d, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DecimalColumn dc = new DecimalColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, d);
                DataStore.activeTable.columns.Add(dc);
                DataStore.batch.Add(dc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterDecimalColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, int d, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckDecimalColumn(name, type, nullAllowed, defaultValue, comment, size, d, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DecimalColumn dc = new DecimalColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size, d);
                DataStore.batch.Add(col.GetAlterColumnStatement(dc, position));
                col.ReplaceCol(dc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateEnumColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string options, out string errorMessage)
        {
            if(!Check.CheckEnumColumn(name, type, nullAllowed, defaultValue, comment, options, out errorMessage))
            {
                return false;
            }
            else
            {
                List<string> opt = options.Trim().Split(",").ToList(); for (int i = 0; i < opt.Count; i++) { opt[i] = opt[i].Trim(); }
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                EnumColumn ec = new EnumColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, opt);
                DataStore.activeTable.columns.Add(ec);
                DataStore.batch.Add(ec.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }
        public static bool AlterEnumColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string options, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckEnumColumn(name, type, nullAllowed, defaultValue, comment, options, out errorMessage))
            {
                return false;
            }
            else
            {
                List<string> opt = options.Trim().Split(",").ToList(); for (int i = 0; i < opt.Count; i++) { opt[i] = opt[i].Trim(); }
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                EnumColumn ec = new EnumColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, opt);
                DataStore.batch.Add(col.GetAlterColumnStatement(ec, position));
                col.ReplaceCol(ec, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateBinaryColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, out string errorMessage)
        {
            if (!Check.CheckBinaryColumn(name, type, nullAllowed, defaultValue, comment, size, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                BinaryColumn bc = new BinaryColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size);
                DataStore.activeTable.columns.Add(bc);
                DataStore.batch.Add(bc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterBinaryColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckBinaryColumn(name, type, nullAllowed, defaultValue, comment, size, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                BinaryColumn bc = new BinaryColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, size);
                DataStore.batch.Add(col.GetAlterColumnStatement(bc, position));
                col.ReplaceCol(bc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateDateTimeColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, out string errorMessage)
        {
            if (!Check.CheckDateTimeColumn(name, type, nullAllowed, defaultValue, comment, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DateTimeColumn dt = new DateTimeColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, "");
                DataStore.activeTable.columns.Add(dt);
                DataStore.batch.Add(dt.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterDateTimeColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string position, Column col, out string errorMessage)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckDateTimeColumn(name, type, nullAllowed, defaultValue, comment, out errorMessage))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DateTimeColumn dt = new DateTimeColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, DataStore.activeTable, "");

                DataStore.batch.Add(col.GetAlterColumnStatement(dt, position));
                col.ReplaceCol(dt, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool DropConstraint(int row, out string errorMessage)
        {
            if (!Check.CheckDropConstraint(row, out errorMessage))
            {
                return false;
            }
            else
            {
                Constraint c = DataStore.activeTable.constraints[row];
                DataStore.batch.Add(c.GetDropStatement());
                DataStore.activeTable.constraints.Remove(c);

                errorMessage = "";
                return true;
            }
        }

        public static bool DropColumn(int row, out string errorMessage)
        {
            if(!Check.CheckDropColumn(row, out errorMessage))
            {
                return false;
            }
            else
            {
                Column c = DataStore.activeTable.columns[row];
                DataStore.batch.Add(c.GetDropStatement());
                DataStore.activeTable.columns.Remove(c);
                errorMessage = "";
                return true;
            }
        }

        public static bool AlterColumn(int row, out string errorMessage)
        {
            if (!Check.CheckAlterColumn(row, out errorMessage))
            {
                return false;
            }
            else return true;
        }
        private static Random random = new Random();

        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void DropAutoIncrement(int row)
        {
            if (DataStore.activeTable.columns[row] is IntegerColumn && (DataStore.activeTable.columns[row] as IntegerColumn).autoincrement)
            {
                IntegerColumn ic = (IntegerColumn)DataStore.activeTable.columns[row];

                ic.autoincrement = false;
                DataStore.batch.Add("ALTER TABLE `" + ic.parent.parent.name + "`.`" + ic.parent.name + "` CHANGE COLUMN `" + ic.name + "` " + ic.GetStatement() + ";");
                for  (int i = 0; i < DataStore.activeTable.constraints.Count; i++)/*(Constraint c in DataStore.activeTable.constraints)*/
                {
                    Constraint c = DataStore.activeTable.constraints[i];
                    if (c is ConstraintK)
                    {
                        ConstraintK k = (ConstraintK)c;
                        if (k.localColumns.Count == 1 && k.localColumns.Contains(ic))
                        {
                            DataStore.activeTable.constraints.Remove(c);
                        }
                    }
                }
            }
        }

        public static bool CreateSimpleConstraint(Choices.ConstraintTypes ct,string name, bool[] arrayColumn, out string errorMessage)
        {
            if(!Check.IsValidName(name, out errorMessage) && ct != Choices.ConstraintTypes.PrimaryKey)
            {
                return false;
            }
            foreach (Constraint c in  DataStore.activeTable.constraints)
            {
                if (c.name == name)
                {
                    errorMessage = "Constraint `" + name + "` already exists.";
                    return false;
                }
            }
            if (GetTrues(arrayColumn) < 1)
            {
                errorMessage = "At least one column must be selected.";
                return false;
            }
            if (ct == Choices.ConstraintTypes.PrimaryKey && DataStore.activeTable.GetPrimaryKey() != null)
            {
                errorMessage = "Primary key of table `" + DataStore.activeTable.name + "` already exists.";
                return false;
            }

            List<Column> list = new List<Column>();
            for (int i = 0; i < DataStore.activeTable.columns.Count; i++)
            {
                if (arrayColumn[i]) list.Add(DataStore.activeTable.columns[i]);
            }

            Constraint k = null;
            if (ct == Choices.ConstraintTypes.Key)
            {
                k = new ConstraintK(DataStore.activeTable, name, list);
            }

            if (ct == Choices.ConstraintTypes.PrimaryKey)
            {
                k = new ConstraintPK(DataStore.activeTable, list);
            }

            if (ct == Choices.ConstraintTypes.UniqueKey)
            {
                k = new ConstraintUQ(DataStore.activeTable, name, list);
            }

            DataStore.activeTable.constraints.Add(k);
            DataStore.batch.Add(k.GetAddStatement());

            errorMessage = "";
            return true;

        }

        public static int GetTrues(bool[] arr)
        {
            int ct = 0;
            if (arr == null) return -1;
            foreach (bool b in arr) { if (b) ct++; }
            return ct;
        }

        public static bool CreateFKConstraint(string name, bool[] arrayColumn, Table remoteTable, bool[] arrayRemoteColumn, out string errorMessage)
        {
            if (!Check.CheckCreateFKConstraint(name, arrayColumn, remoteTable, arrayRemoteColumn, out errorMessage))
            {
                return false;
            }
            else
            {
                Column localColumn = DataStore.activeTable.columns[Array.IndexOf(arrayColumn, true)];
                Column remoteColumn = remoteTable.columns[Array.IndexOf(arrayRemoteColumn, true)];

                ConstraintK k = new ConstraintK(remoteTable, remoteColumn.name + "_KEY_" + GetRandomString(10), new List<Column>() { remoteColumn });
                remoteTable.constraints.Add(k);
                DataStore.batch.Add(k.GetAddStatement());

                ConstraintFK fk = new ConstraintFK(DataStore.activeTable, name, new List<Column>() { localColumn}, new List<Column>() { remoteColumn }, remoteTable, "", "");
                DataStore.activeTable.constraints.Add(fk);
                DataStore.batch.Add(fk.GetAddStatement());

                errorMessage = "";
                return true;
            }
        }

        public static void CreateBatchDeNovo()
        {
            List<string> list = new List<string>();

            foreach (Database db in DataStore.databases)
            {
                if (db.name == "sys" || db.name == "information_schema" || db.name == "performance_schema" || db.name == "mysql") continue;
                list.Add(db.GetDropStatement());
                list.Add(db.GetStatement());
            }

            foreach (Database db in DataStore.databases)
            {
                if (db.name == "sys" || db.name == "information_schema" || db.name == "performance_schema" || db.name == "mysql") continue;
                foreach (Table table in db.tables)
                {
                    
                    list.Add(table.GetStatement());
                }
            }

            DataStore.batchDeNovo = list;
        }
    }
}
