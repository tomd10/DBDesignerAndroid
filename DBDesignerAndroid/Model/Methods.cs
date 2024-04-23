namespace DBDesignerWIP
{
    public static class Methods
    {
        public static void LoadDatabases(DbService ctx)
        {
            foreach (string database in ctx.dbConnection.GetDatabases())
            {
                string err;
                Database db = new Database(ctx.dbConnection.GetCreateDatabase(database));
                if (db.name != "information_schema" && db.name != "mysql" && db.name != "performance_schema" && db.name != "sys" && db.name != "sakila" && Check.IsValidName(db.name, out err))
                {
                    ctx.databases.Add(db);
                }
                else continue;

                List<string> tables = ctx.dbConnection.GetTables(database);
                foreach (string table in tables)
                {
                    db.tables.Add(new Table(db, ctx.dbConnection.GetCreateTable(database, table)));
                }
            }

            foreach (Database db in ctx.databases)
            {
                db.LoadConstraints();
            }
        }

        public static Database GetNthDatabase(int n, DbService ctx)
        {
            if (n < 0 || n >= ctx.databases.Count)
            {
                return null;
            }
            else return ctx.databases[n];
        }

        public static bool CreateDatabase(string name, string charset, string collate, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckDatabaseName(name, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                Database db = new Database(name, charset, charset + collate, new List<Table>());
                ctx.databases.Add(db);

                ctx.batch.Add(db.GetStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateTable(string name, bool isTemporary, string engine, string charset, string collate, int auto_increment, string comment, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckTableName(name, out errorMessage, ctx))
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
                Table t = new Table(name, isTemporary, engine, charset, collate, auto_increment.ToString(), comment, ctx.activeDatabase);
                t.CreateDefaultColumn();
                ctx.activeDatabase.tables.Add(t);

                ctx.batch.Add(t.GetStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterTable(Table table, string name, int autoIncrement, string charset, string collate, string engine, string comment, out string errorMessage, DbService ctx)
        {
            if (table.name != name && !Check.CheckTableName(name, out errorMessage, ctx))
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
                    ctx.batch.Add(table.GetAlterName(name));
                    table.name = name;
                }
                if (table.engine != engine || int.Parse(table.auto_increment) != autoIncrement || table.collate != collate || table.charset != charset || table.comment != comment)
                {
                    table.engine = engine;
                    table.auto_increment = autoIncrement.ToString();
                    table.collate = collate;
                    table.charset = charset;
                    table.comment = comment;
                    ctx.batch.Add(table.GetAlterStatement());
                }


                errorMessage = "";
                return true;
            }
        }

        public static void DropDatabase (int n, out string name, DbService ctx)
        {
            Database db = GetNthDatabase(n, ctx);
            name = db.name;
            ctx.batch.Add(db.GetDropStatement());

            if (ctx.activeTable != null && ctx.activeTable.parent == db) ctx.activeTable = null;
            if (ctx.activeDatabase == db) ctx.activeDatabase = null;

            ctx.databases.Remove(db);
        }

        public static bool DropTable(int n, out string name, out string errorMessage, DbService ctx)
        {
            Table t = ctx.activeDatabase.GetNthTable(n);
            List<ConstraintFK> constraints = ctx.activeDatabase.GetTableFKReference(t);
            name = t.name;
            if (constraints.Count == 0)
            {
                errorMessage = "";
                ctx.batch.Add(t.GetDropStatement());
                ctx.activeDatabase.tables.Remove(t);
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

        public static bool CreateTextColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string charset, string collate, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckTextColumn(name, type, nullAllowed, defaultValue, comment, size, charset, collate, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                TextColumn tc = new TextColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, charset, charset + collate);
                ctx.activeTable.columns.Add(tc);
                ctx.batch.Add(tc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterTextColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string charset, string collate, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckTextColumn(name, type, nullAllowed, defaultValue, comment, size, charset, collate, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                TextColumn tc = new TextColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, charset, charset + collate);
                ctx.batch.Add(col.GetAlterColumnStatement(tc, position));
                col.ReplaceCol(tc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateIntegerColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, bool unsigned, bool zerofill, bool autoIncrement, out string errorMessage, DbService ctx)
        {
            if(!Check.CheckIntegerColumn(name, type, nullAllowed, defaultValue, comment, size, unsigned, zerofill, autoIncrement, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                IntegerColumn ic = new IntegerColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, unsigned, zerofill, autoIncrement);
                if (autoIncrement)
                {
                    ic.autoincrement = false;
                    ctx.activeTable.columns.Add(ic);
                    ctx.batch.Add(ic.GetAddColumnStatement());
                    ConstraintK key = new ConstraintK(ctx.activeTable, name + "_KEY_" + GetRandomString(10), new List<Column>() { ic });
                    ctx.batch.Add(key.GetAddStatement());
                    ctx.activeTable.constraints.Add(key);
                    ic.autoincrement = true;
                    ctx.batch.Add("ALTER TABLE `" + ic.parent.parent.name + "`.`" + ic.parent.name + "` CHANGE COLUMN `" + ic.name + "` " + ic.GetStatement() + " ;");
                }
                else
                {
                    ctx.activeTable.columns.Add(ic);
                    ctx.batch.Add(ic.GetAddColumnStatement());
                }


                errorMessage = "";
                return true;
            }
        }


        public static bool AlterIntegerColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, bool unsigned, bool zerofill, bool autoIncrement, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (col is IntegerColumn) { (col as IntegerColumn).autoincrement = false; }
            if (!Check.CheckIntegerColumn(name, type, nullAllowed, defaultValue, comment, size, unsigned, zerofill, autoIncrement, out errorMessage, ctx))
            {
                return false;
            }
            else
            {

                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                IntegerColumn ic = new IntegerColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, unsigned, zerofill, autoIncrement);
                ctx.batch.Add(col.GetAlterColumnStatement(ic, position));
                col.ReplaceCol(ic, position);
                errorMessage = "";
                return true;
            }
        }

        public static bool CreateDecimalColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, int d, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckDecimalColumn(name, type, nullAllowed, defaultValue, comment, size, d, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DecimalColumn dc = new DecimalColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, d);
                ctx.activeTable.columns.Add(dc);
                ctx.batch.Add(dc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterDecimalColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, int d, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckDecimalColumn(name, type, nullAllowed, defaultValue, comment, size, d, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DecimalColumn dc = new DecimalColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size, d);
                ctx.batch.Add(col.GetAlterColumnStatement(dc, position));
                col.ReplaceCol(dc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateEnumColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string options, out string errorMessage, DbService ctx)
        {
            if(!Check.CheckEnumColumn(name, type, nullAllowed, defaultValue, comment, options, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                List<string> opt = options.Trim().Split(",").ToList(); for (int i = 0; i < opt.Count; i++) { opt[i] = opt[i].Trim(); }
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                EnumColumn ec = new EnumColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, opt);
                ctx.activeTable.columns.Add(ec);
                ctx.batch.Add(ec.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }
        public static bool AlterEnumColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string options, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckEnumColumn(name, type, nullAllowed, defaultValue, comment, options, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                List<string> opt = options.Trim().Split(",").ToList(); for (int i = 0; i < opt.Count; i++) { opt[i] = opt[i].Trim(); }
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                EnumColumn ec = new EnumColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, opt);
                ctx.batch.Add(col.GetAlterColumnStatement(ec, position));
                col.ReplaceCol(ec, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateBinaryColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckBinaryColumn(name, type, nullAllowed, defaultValue, comment, size, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                BinaryColumn bc = new BinaryColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size);
                ctx.activeTable.columns.Add(bc);
                ctx.batch.Add(bc.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterBinaryColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, int size, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckBinaryColumn(name, type, nullAllowed, defaultValue, comment, size, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                BinaryColumn bc = new BinaryColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, size);
                ctx.batch.Add(col.GetAlterColumnStatement(bc, position));
                col.ReplaceCol(bc, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool CreateDateTimeColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckDateTimeColumn(name, type, nullAllowed, defaultValue, comment, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DateTimeColumn dt = new DateTimeColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, "");
                ctx.activeTable.columns.Add(dt);
                ctx.batch.Add(dt.GetAddColumnStatement());

                errorMessage = "";
                return true;
            }
        }

        public static bool AlterDateTimeColumn(string name, string type, bool nullAllowed, string defaultValue, string comment, string position, Column col, out string errorMessage, DbService ctx)
        {
            col.name = "TEMP " + col.name;
            if (!Check.CheckDateTimeColumn(name, type, nullAllowed, defaultValue, comment, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                string? defa = (defaultValue.ToUpper() == "#NULL") ? null : defaultValue;
                bool defaultValueSupported = !(defaultValue == "");
                DateTimeColumn dt = new DateTimeColumn(name, nullAllowed, type, defaultValueSupported, defa, comment, ctx.activeTable, "");

                ctx.batch.Add(col.GetAlterColumnStatement(dt, position));
                col.ReplaceCol(dt, position);

                errorMessage = "";
                return true;
            }
        }

        public static bool DropConstraint(int row, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckDropConstraint(row, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                Constraint c = ctx.activeTable.constraints[row];
                ctx.batch.Add(c.GetDropStatement());
                ctx.activeTable.constraints.Remove(c);

                errorMessage = "";
                return true;
            }
        }

        public static bool DropColumn(int row, out string errorMessage, DbService ctx)
        {
            if(!Check.CheckDropColumn(row, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                Column c = ctx.activeTable.columns[row];
                ctx.batch.Add(c.GetDropStatement());
                ctx.activeTable.columns.Remove(c);
                errorMessage = "";
                return true;
            }
        }

        public static bool AlterColumn(int row, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckAlterColumn(row, out errorMessage, ctx))
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

        public static void DropAutoIncrement(int row, DbService ctx)
        {
            if (ctx.activeTable.columns[row] is IntegerColumn && (ctx.activeTable.columns[row] as IntegerColumn).autoincrement)
            {
                IntegerColumn ic = (IntegerColumn)ctx.activeTable.columns[row];

                ic.autoincrement = false;
                ctx.batch.Add("ALTER TABLE `" + ic.parent.parent.name + "`.`" + ic.parent.name + "` CHANGE COLUMN `" + ic.name + "` " + ic.GetStatement() + ";");
                for  (int i = 0; i < ctx.activeTable.constraints.Count; i++)/*(Constraint c in ctx.activeTable.constraints)*/
                {
                    Constraint c = ctx.activeTable.constraints[i];
                    if (c is ConstraintK)
                    {
                        ConstraintK k = (ConstraintK)c;
                        if (k.localColumns.Count == 1 && k.localColumns.Contains(ic))
                        {
                            ctx.activeTable.constraints.Remove(c);
                        }
                    }
                }
            }
        }

        public static bool CreateSimpleConstraint(Choices.ConstraintTypes ct,string name, bool[] arrayColumn, out string errorMessage, DbService ctx)
        {
            if(!Check.IsValidName(name, out errorMessage) && ct != Choices.ConstraintTypes.PrimaryKey)
            {
                return false;
            }
            foreach (Constraint c in  ctx.activeTable.constraints)
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
            if (ct == Choices.ConstraintTypes.PrimaryKey && ctx.activeTable.GetPrimaryKey() != null)
            {
                errorMessage = "Primary key of table `" + ctx.activeTable.name + "` already exists.";
                return false;
            }

            List<Column> list = new List<Column>();
            for (int i = 0; i < ctx.activeTable.columns.Count; i++)
            {
                if (arrayColumn[i]) list.Add(ctx.activeTable.columns[i]);
            }

            Constraint k = null;
            if (ct == Choices.ConstraintTypes.Key)
            {
                k = new ConstraintK(ctx.activeTable, name, list);
            }

            if (ct == Choices.ConstraintTypes.PrimaryKey)
            {
                k = new ConstraintPK(ctx.activeTable, list);
            }

            if (ct == Choices.ConstraintTypes.UniqueKey)
            {
                k = new ConstraintUQ(ctx.activeTable, name, list);
            }

            ctx.activeTable.constraints.Add(k);
            ctx.batch.Add(k.GetAddStatement());

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

        public static bool CreateFKConstraint(string name, bool[] arrayColumn, Table remoteTable, bool[] arrayRemoteColumn, out string errorMessage, DbService ctx)
        {
            if (!Check.CheckCreateFKConstraint(name, arrayColumn, remoteTable, arrayRemoteColumn, out errorMessage, ctx))
            {
                return false;
            }
            else
            {
                Column localColumn = ctx.activeTable.columns[Array.IndexOf(arrayColumn, true)];
                Column remoteColumn = remoteTable.columns[Array.IndexOf(arrayRemoteColumn, true)];

                ConstraintK k = new ConstraintK(remoteTable, remoteColumn.name + "_KEY_" + GetRandomString(10), new List<Column>() { remoteColumn });
                remoteTable.constraints.Add(k);
                ctx.batch.Add(k.GetAddStatement());

                ConstraintFK fk = new ConstraintFK(ctx.activeTable, name, new List<Column>() { localColumn}, new List<Column>() { remoteColumn }, remoteTable, "", "");
                ctx.activeTable.constraints.Add(fk);
                ctx.batch.Add(fk.GetAddStatement());

                errorMessage = "";
                return true;
            }
        }

        public static void CreateBatchDeNovo(DbService ctx)
        {
            List<string> list = new List<string>();

            foreach (Database db in ctx.databases)
            {
                if (db.name == "sys" || db.name == "information_schema" || db.name == "performance_schema" || db.name == "mysql") continue;
                list.Add(db.GetDropStatement());
                list.Add(db.GetStatement());
            }

            
            foreach (Database db in ctx.databases)
            {
                if (db.name == "sys" || db.name == "information_schema" || db.name == "performance_schema" || db.name == "mysql") continue;
                List<Table> processedTables = new List<Table>();

                int processed = 0;

                foreach (Table t in db.tables)
                {
                    Console.WriteLine("---" + t.name);
                }

                //#1 Tables unreferenced by FOREIGN KEY
                for(int i = 0; i<db.tables.Count; i++)
                {
                    bool match = false;

                    foreach (Constraint c in db.tables[i].constraints)
                    {
                        if (c is ConstraintFK)
                        {
                            ConstraintFK fk = (ConstraintFK)c;
                            if (fk.parent != db.tables[i]) continue;
                            if (!processedTables.Contains(fk.remoteTable))
                            {
                                match = true;
                                break;
                            }
                        }
                    }

                    if (!match)
                    {
                        list.Add(db.tables[i].GetStatement());
                        processedTables.Add(db.tables[i]);
                        processed++;
                        Console.Write(db.tables[i].name + " ");
                    }
                }

                Console.WriteLine();
                //#2 Tables referenced by 
                while (processed < db.tables.Count)
                {
                    for (int i = 0; i < db.tables.Count; i++)
                    {
                        if (processedTables.Contains(db.tables[i])) { continue; }
                        List<ConstraintFK> listFK = db.GetTableFKReference(db.tables[i]);

                        Console.WriteLine("processing" + db.tables[i].name +" start");
                        bool match = false;
                        foreach (Constraint c in db.tables[i].constraints)
                        {
                            if (c is ConstraintFK)
                            {
                                ConstraintFK fk = (ConstraintFK)c;
                                if (fk.parent != db.tables[i]) continue;
                                if (!processedTables.Contains(fk.remoteTable))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                        /*
                        {
                            Console.WriteLine(constraintFK.remoteTable.name);
                            if (!processedTables.Contains(constraintFK.remoteTable))
                            {
                                match = true;
                            }
                        }
                        Console.WriteLine("end");
                        */
                        if (match == false)
                        {
                            processedTables.Add(db.tables[i]);
                            processed++;
                            list.Add(db.tables[i].GetStatement());
                        }
                    }
                }
                
            }

            ctx.batchDeNovo = list;
        }
    }
}
