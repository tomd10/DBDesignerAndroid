namespace DBDesignerWIP
{
    public class Table
    {
        public string name { get; set; } = "";
        public bool isTemporary { get; set; } = false;
        public string engine { get; set; } = "";
        public string charset { get; set; } = "";
        public string collate { get; set; } = "";
        public string auto_increment { get; set; } = "";
        public string comment { get; set; } = "";
        public Database parent { get; set; }
        public List<Constraint> constraints { get; set; } = new List<Constraint>();
        public List<Column> columns { get; set; } = new List<Column>();
        private List<string> constraintDefs = new List<string>();

        public Table(string name, bool isTemporary, string engine, string charset, string collate, string auto_increment, string comment, Database parent)
        {
            this.isTemporary = isTemporary;
            this.name = name;
            this.engine = engine;
            this.charset = charset;
            this.collate = collate;
            this.auto_increment = auto_increment;
            this.comment = comment;
            this.parent = parent;
        }

        public Table(Database parent,string cmd)
        {
            this.name = StatementText.GetTableName(cmd);
            this.isTemporary = StatementText.GetTableTemporary(cmd);
            this.engine = StatementText.GetTableValue(cmd, "ENGINE");
            this.collate = StatementText.GetTableValue(cmd, "COLLATE");
            this.charset = StatementText.GetTableValue(cmd, "CHARSET");
            this.auto_increment = StatementText.GetTableValue(cmd, "AUTO_INCREMENT");
            this.comment = StatementText.GetTableValue(cmd, "COMMENT").Replace("'", "").Replace("\"", "").Replace("`", "");
            this.constraintDefs = Text.GetConstraintDefs(cmd);
            foreach (string columnDef in Text.GetColumnDefs(cmd))
            {
                columns.Add(Column.CreateColumn(this, columnDef));
            }
            this.parent = parent;
        }

        public Column GetColumnByName(string s)
        {
            foreach (Column c in columns)
            {
                if (s == c.name) return c;
            }
            return null;
        }

        public void LoadConstraints()
        {
            foreach (string s in constraintDefs)
            {

                /*
                 * not impll
                 * 
                 */ 
                if (s.Split(" ")[0] == "FULLTEXT" || s.Split(" ")[0] == "SPATIAL") continue;
                constraints.Add(Constraint.CreateConstraint(this, s));
            }
        }

        public string GetStatement()
        {
            string result = "CREATE";
            if (isTemporary) result = result + " TEMPORARY";
            result = result + " TABLE IF NOT EXISTS `" + parent.name + "`.`" + name + "` (\n";
            foreach (Column c in columns)
            {
                result = result + c.GetStatement() + " ,\n";
            }
            foreach (Constraint c in constraints)
            {
                result = result + c.GetStatement() + " ,\n";
            }
            result = result.Substring(0,result.Length - 2);
            result = result + "\n) ";
            result = result + "ENGINE=" + engine + " AUTO_INCREMENT=" + auto_increment + " DEFAULT CHARSET=" + charset + " COLLATE=" + collate + " COMMENT='" + comment + "';";
            Console.WriteLine("#######" + comment);
            return result;
        }

        public string GetDropStatement()
        {
            return "DROP TABLE IF EXISTS `"+ parent.name + "`.`"+name+"`;";
        }

        public bool GetColumnNameAvailable(string s)
        {
            foreach (Column c in columns)
            {
                if (c.name.ToLower() == s.ToLower()) return false;
            }
            return true;
        }

        public ConstraintPK GetPrimaryKey()
        {
            foreach (Constraint c in constraints)
            {
                if (c is ConstraintPK) return (ConstraintPK)c;
            }

            return null;
        }

        public List<ConstraintUQ> GetUniqueKeys()
        {
            List<ConstraintUQ> list = new List<ConstraintUQ>();
            foreach (Constraint c in constraints)
            {
                if (c is ConstraintUQ) list.Add((ConstraintUQ)c);
            }

            return list;
        }

        public List<ConstraintFK> GetForeignKeys()
        {
            List<ConstraintFK> list = new List<ConstraintFK>();
            foreach (Constraint c in constraints)
            {
                if (c is ConstraintFK) list.Add((ConstraintFK)c);
            }

            return list;
        }


        public void ReplaceColumn(Column oldCol, Column newCol)
        {
            int i = columns.IndexOf(oldCol);
            if (i > -1)
            {
                columns[i] = newCol;
            }
        }

        public string GetModifyColumn(Column oldCol, Column newCol)
        {
            return "ALTER TABLE `" + newCol.name + "` CHANGE COLUMN `" + oldCol.name + "` " + newCol.GetStatement() + ";";
        }

        public bool GetAutoIncrement()
        {
            foreach (Column c in columns)
            {
                if (c is IntegerColumn)
                {
                    IntegerColumn ic = (IntegerColumn)c;
                    if (ic.autoincrement) return true;
                }
            }
            return false;
        }

        public List<Constraint> GetConstraintsOfColumn(Column col)
        {
            List<Constraint> list = new List<Constraint>();
            foreach (Constraint c in constraints)
            {
                if (c is ConstraintPK)
                {
                    ConstraintPK cp = (ConstraintPK)c;
                    if (cp.localColumns.Contains(col)) list.Add(cp);
                }
                if (c is ConstraintK)
                {
                    ConstraintK cp = (ConstraintK)c;
                    if (cp.localColumns.Contains(col)) list.Add(cp);
                }
                if (c is ConstraintUQ)
                {
                    ConstraintUQ cp = (ConstraintUQ)c;
                    if (cp.localColumns.Contains(col)) list.Add(cp);
                }
                if (c is ConstraintFK)
                {
                    ConstraintFK cp = (ConstraintFK)c;
                    if (cp.localColumns.Contains(col))list.Add(cp);
                }
            }

            return list;
        }

        public void CreateDefaultColumn()
        {
            IntegerColumn ic = new IntegerColumn("DefaultColDbDes", true, "INT", false, null, "DBDesigner default column", this, 11, false, false, false);
            this.columns.Add(ic);
        }

        public string GetAlterStatement()
        {
            return "ALTER TABLE `" + this.parent.name + "`.`" + name + "` ENGINE=" + engine + " AUTO_INCREMENT=" + auto_increment + " DEFAULT CHARSET=" + charset + " COLLATE=" + collate + " COMMENT='" + comment + "';";
        }

        public string GetAlterName(string newName)
        {
            return "ALTER TABLE `" + this.parent.name + "`.`" + name + "` RENAME '" + newName + "';"; 
        }
    }
}
