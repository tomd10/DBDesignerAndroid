namespace DBDesignerWIP
{

    public class Column
    {
        public string name { get; set; } = "";
        public bool nullAllowed { get; set; } = true;
        public string type { get; set; } = "";
        public bool defaultValueSupported { get; set; } = false;
        public string? defaultValue {  get; set; } = null;
        public string comment { get; set; } = "";
        public Table parent { get; set; }

        /*
        public int size { get; set; } = 0;
        public int d { get; set; } = 0;
        public List<string> fields { get; set; } = new List<string>();
        public bool unsigned { get; set; } = false;
        public string collate { get; set; } = "";
        public string charset { get; set; } = "";
        */

        public Column (Table parent, string cmd)
        {
            this.parent = parent;
        }

        public Column(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.parent = parent;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
        }

        public virtual string GetStatement()
        {
            return "";
        }

        public string GetDropStatement()
        {
            return "ALTER TABLE `" + parent.parent.name + "`.`" + parent.name+ "` DROP COLUMN `" + name + "` ;"; 
        }

        public static Column CreateColumn(Table parent, string s)
        {
            string type = Text.GetColumnType(s);
            if (type.Contains("INT"))
            {
                return new IntegerColumn(parent, s);
            }
            if (type.Contains("TEXT") || type.Contains("CHAR"))
            {
                return new TextColumn(parent, s);
            }
            if (type == "SET" || type == "ENUM")
            {
                return new EnumColumn(parent, s);
            }
            if (type == "DEC" || type == "DECIMAL" || type == "FLOAT" || type == "DOUBLE")
            {
                return new DecimalColumn(parent, s);
            }
            if (type.Contains("BLOB") || type.Contains("BINARY"))
            {
                return new BinaryColumn(parent, s);
            }
            if (type == "DATE" || type == "TIME" || type == "DATETIME" || type == "TIMESTAMP" || type == "YEAR")
            {
                return new DateTimeColumn(parent, s);
            }

            throw new NotImplementedException("Unsupported column type.");
        }

        public List<Constraint> GetColumnReferences()
        {
            List<Constraint> list = new List<Constraint>();
            foreach (Constraint c in parent.constraints)
            {
                if (c.localColumns.Contains(this)) list.Add(c);
            }

            return list;
        }

        public string GetAddColumnStatement()
        {
            return "ALTER TABLE `" + parent.parent.name + "`.`" + parent.name + "` ADD COLUMN " + GetStatement() + " ;";
        }

        public string GetAlterColumnStatement(Column newCol, string position)
        {
            string ret;
            if (this.name.Split(" ").Length == 1)
            {
                ret = "ALTER TABLE `" + parent.parent.name + "`.`" + parent.name + "` CHANGE COLUMN `" + name + "` " + newCol.GetStatement();
            }
            else
            {
                ret = "ALTER TABLE `" + parent.parent.name + "`.`" + parent.name + "` CHANGE COLUMN `" + name.Split(" ")[1] + "` " + newCol.GetStatement();
            }
            
            if (position != null && position != "")
            {
                ret = ret + " " + position + ";";
            }
            else ret = ret + ";";

            return ret;
        }

        public void ReplaceCol(Column newCol, string position) 
        {
            if (position == "" || position == null)
            {
                parent.columns.Add(newCol);
                parent.columns.Remove(this);
            }
            else if (position == "FIRST")
            {
                parent.columns.Insert(0, newCol);
                parent.columns.Remove(this);
            }
            else
            {
                string name = position.Split(" ")[1];
                Column c = parent.GetColumnByName(name);
                int index = Array.IndexOf(parent.columns.ToArray(), c);
                if (index != -1)
                {
                    parent.columns.Insert(index+1, newCol);
                    parent.columns.Remove(this);
                }
            }
        }

    }

    public class TextColumn : Column
    {
        public int size { get; set; } = 1;
        public string charset { get; set; } = "";
        public string collate { get; set; } = "";

        public TextColumn(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, int size, string charset, string collate)
            : base(name, nullAllowed, type, defaultValueSupported, defaultValue, comment, parent)
        {
            this.size = size;
            this.charset = charset;
            this.collate = collate;
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            this.comment = comment;
        }

        public TextColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            size = StatementText.GetIntegerSize(cmd);
            defaultValueSupported = StatementText.GetStringDefault(cmd, out dv);
            defaultValue = dv;
            nullAllowed = StatementText.GetNullAllowed(cmd);
            comment = StatementText.GetComment(cmd);
            charset = StatementText.GetCharset(cmd);
            collate = StatementText.GetCollate(cmd);
            this.parent = parent;
        }

        public override string GetStatement()
        {
            string result = "  `" + name + "` ";
            if (type.ToUpper() == "CHAR" || type.ToUpper() == "VARCHAR")
            {
                result = result + type + "(" + size.ToString() + ") ";
                
            }
            else
            {
                result = result + type + " ";
            }
            if (charset != "") result = result + "CHARACTER SET '" + charset + "' ";
            if (collate != "") result = result + "COLLATE '" + collate + "' ";
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT '" + defaultValue + "' "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();


            return result;
        }

    }

    public class EnumColumn : Column
    {
        public List<string> options { get; set; } = new List<string>();
        public EnumColumn(string name, bool nullAllowed,  string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, List<string> fields)
        : base(name, nullAllowed, type, defaultValueSupported, defaultValue, comment, parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            this.options = fields;
            this.comment = comment;
        }

        public EnumColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            defaultValueSupported = StatementText.GetStringDefault(cmd, out dv);
            defaultValue = dv;
            nullAllowed = StatementText.GetNullAllowed(cmd);
            comment = StatementText.GetComment(cmd);
            if (type == "ENUM") options = StatementText.GetEnumOptions(cmd);
            if (type == "SET") options = StatementText.GetSetOptions(cmd);
            this.parent = parent;
        }

        public override string GetStatement()
        {
            string result = "  `" + name + "` " + type +"(";
            foreach (string field in options) { result = result + "'" + field + "',"; }
            result = result.Substring(0, result.Length - 1);
            result = result + ") ";
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT '" + defaultValue + "' "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();


            return result;
        }

        public string GetOptions()
        {
            string result = "";
            foreach (string field in options) { result = result + "'" + field + "',"; }
            return result.Substring(0, result.Length - 1);
        }
    }

    public class IntegerColumn : Column
    {
        public int size { get; set; } = 0;
        public bool zerofill { get; set; } = false;
        public bool unsigned {  get; set; } = false;
        public bool autoincrement { get; set; } = false;

        public IntegerColumn(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, int size, bool unsigned, bool zerofill, bool autoincrement)
        : base(name, nullAllowed, type, defaultValueSupported, defaultValue,  comment, parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            this.size = size;
            this.zerofill = zerofill;
            this.unsigned = unsigned;
            this.autoincrement = autoincrement;
            this.parent = parent;
            this.comment = comment;
        }

        public IntegerColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            size = StatementText.GetIntegerSize(cmd);
            defaultValueSupported = StatementText.GetIntegerDefault(cmd, out dv);
            defaultValue = dv;
            nullAllowed = StatementText.GetNullAllowed(cmd);
            zerofill = StatementText.GetZerofill(cmd);
            unsigned = StatementText.GetUnsigned(cmd);
            autoincrement = StatementText.GetAutoIncrement(cmd);
            comment = StatementText.GetComment(cmd);
            this.parent = parent;     
        }
        public override string GetStatement()
        {
            string result = "  `" + name + "` " + type + "(" + size + ") ";
            if (unsigned) result = result + "UNSIGNED ";
            if (zerofill) result = result + "ZEROFILL ";
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT " + defaultValue + " "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (autoincrement) result = result + "AUTO_INCREMENT ";
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();


            return result;
        }
    }

    public class DecimalColumn : Column
    {
        public int size { get; set; } = 0;
        public int d { get; set; } = 0;

        public DecimalColumn(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, int size, int d)
        : base(name, nullAllowed, type, defaultValueSupported, defaultValue, comment, parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            this.size = size;
            this.d = d;
            this.comment = comment;
        }

        public DecimalColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            int display = 0;
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            size = StatementText.GetIntegerSize(cmd);
            defaultValueSupported = StatementText.GetDecimalDefault(cmd, out dv);
            defaultValue = dv;
            nullAllowed = StatementText.GetNullAllowed(cmd);
            size = StatementText.GetDecimalSize(cmd, out display);
            d = display;
            comment = StatementText.GetComment(cmd);
            this.parent = parent;
        }

        public override string GetStatement()
        {
            string result = "  `" + name + "` " + type + "(" + size + "," + d + ") ";
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT " + defaultValue.Replace(",", ".") + " "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();

            return result;
        }
    }

    public class  BinaryColumn : Column
    {
        public int size { get; set; } = 0;

        public BinaryColumn(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, int size)
        : base(name, nullAllowed, type, defaultValueSupported, defaultValue, comment, parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            this.size = size;
            this.comment = comment;
        }

        public BinaryColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            this.parent = parent;
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            if (type == "BINARY" || type == "VARBINARY")
            {
                this.size = StatementText.GetIntegerSize(cmd);
            }
            defaultValueSupported = StatementText.GetStringDefault(cmd, out dv);
            nullAllowed = StatementText.GetNullAllowed(cmd);
            defaultValue = dv;

        }

        public override string GetStatement()
        {
            string result = "  `" + name + "` ";
            if (type.ToUpper() == "BINARY" || type.ToUpper() == "VARBINARY")
            {
                result = result + type + "(" + size.ToString() + ") ";
            }
            else
            {
                result = result + type + " ";
            }
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT '" + defaultValue + "' "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();

            return result;
        }
    }

    public class DateTimeColumn : Column
    {
        //public string format { get; set; } = "";
        public DateTimeColumn(string name, bool nullAllowed, string type, bool defaultValueSupported, string? defaultValue, string comment, Table parent, string format)
        : base(name, nullAllowed, type, defaultValueSupported, defaultValue, comment, parent)
        {
            this.name = name;
            this.nullAllowed = nullAllowed;
            this.type = type;
            this.defaultValueSupported = defaultValueSupported;
            this.defaultValue = defaultValue;
            this.parent = parent;
            //this.format = format;
            this.comment = comment;
        }

        public DateTimeColumn(Table parent, string cmd) : base(parent, cmd)
        {
            string dv = "";
            this.parent = parent;
            name = StatementText.GetName(cmd);
            type = StatementText.GetType(cmd);
            defaultValueSupported = StatementText.GetStringDefault(cmd, out dv);
            defaultValue = dv;
            nullAllowed = StatementText.GetNullAllowed(cmd);
        }

        public override string GetStatement()
        {
            string result = "  `" + name + "` " + type + " ";
            if (!nullAllowed) result = result + "NOT NULL ";
            if (defaultValueSupported)
            {
                if (defaultValue != null) { result = result + "DEFAULT '" + defaultValue + "' "; }
                else { result = result + "DEFAULT NULL "; }
            }
            if (comment != "")
            {
                result = result + "COMMENT '" + comment + "'";
            }
            result = result.Trim();

            return result;
        }


    }
}
