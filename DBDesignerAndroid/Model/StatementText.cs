namespace DBDesignerWIP
{
    public static class StatementText
    {
        public static string GetName(string cmd)
        {
            return cmd.Split(' ')[0].Replace("`", "");
        }

        public static string GetType(string cmd)
        {
            return Text.ToBracket(cmd.Split(" ")[1].ToUpper());
        }

        public static int GetIntegerSize(string cmd)
        {
            int s = -1;
            string type = GetType(cmd);
            s = int.TryParse(Text.BracketsContent(cmd.Split(" ")[1]), out s) ? s : -1;
            if (s == -1)
            {
                if (type == "INT") s = 11;
                if (type == "BIGINT") s = 20;
                if (type == "SMALLINT") s = 6;
                if (type == "TINYINT") s = 4;
                if (type == "MEDIUMINT") s = 9;
            }
            return s;
        }

        
        public static bool GetIntegerDefault(string cmd, out string defaultValue)
        {
            string result = Text.IntegerLiteralAfter(cmd, "DEFAULT");
            if (result == "")
            {
                defaultValue = result;
                return false;
            }
            else
            {
                defaultValue = result;
                return true;
            }
        }

        public static bool GetDecimalDefault(string cmd, out string defaultValue)
        {
            string result = Text.DecimalLiteralAfter(cmd, "DEFAULT");
            if (result == "")
            {
                defaultValue = result;
                return false;
            }
            else
            {
                defaultValue = result;
                return true;
            }
        }

        /*unique = words.Contains("UNIQUE");
            nullAllowed = !(words.Contains("NOT") && words[Array.IndexOf(words, "NOT")+1] == "NULL");*/

        public static bool GetUnique(string cmd)
        {
            return cmd.ToUpper().Split(" ").Contains("UNIQUE");
        }

        public static bool GetNullAllowed(string cmd)
        {
            string[] words = cmd.ToUpper().Split(" ");
            return !(words.Contains("NOT") && words[Array.IndexOf(words, "NOT") + 1] == "NULL");
        }

        public static bool GetAutoIncrement(string cmd)
        {
            return cmd.ToUpper().Split(" ").Contains("AUTO_INCREMENT");
        }

        public static bool GetZerofill(string cmd)
        {
            return cmd.ToUpper().Split(" ").Contains("ZEROFILL");
        }

        public static bool GetUnsigned(string cmd)
        {
            return cmd.ToUpper().Split(" ").Contains("UNSIGNED");
        }

        public static string GetComment(string cmd)
        {
            return Text.QuotedStringAfter(cmd, "COMMENT ");
        }

        public static List<string> GetEnumOptions(string cmd)
        {
            string optionString = Text.BracketedStringAfter(cmd, "ENUM");
            if (optionString == "") optionString = Text.BracketedStringAfter(cmd, "enum");
            string[] options = optionString.Split(',');
            List<string> opt = new List<string>();
            foreach (string s in options)
            {
                opt.Add(s.Replace("\'", "").Trim());
            }

            return opt;
        }

        public static List<string> GetSetOptions(string cmd)
        {
            string optionString = Text.BracketedStringAfter(cmd, "SET");
            if (optionString == "") optionString = Text.BracketedStringAfter(cmd, "set");
            string[] options = optionString.Split(',');
            List<string> opt = new List<string>();
            foreach (string s in options)
            {
                opt.Add(s.Replace("\'", "").Trim());
            }

            return opt;
        }

        public static bool GetStringDefault(string cmd, out string defaultValue)
        {
            string result = Text.QuotedStringAfter(cmd, "DEFAULT ");
            string[] words = cmd.Trim().ToUpper().Split(" ");
            int i = Array.IndexOf(words, "DEFAULT");
            if (i + 1 < words.Length && words[i+1].Trim().ToUpper() == "\'\'")
            {
                defaultValue = "";
                return true;
            }
            if (i + 1 < words.Length && words[i + 1].Trim().ToUpper() == "NULL")
            {
                defaultValue = null;
                return true;
            }
            if (result == "")
            {
                defaultValue = result;
                return false;
            }
            else
            {
                defaultValue = result;
                return true;
            }
        }

        public static string GetCharset(string cmd)
        {
            string[] words = cmd.ToUpper().Split(' ');
            if (words.Contains("CHARACTER"))
            {
                int index = Array.IndexOf(words, "CHARACTER");
                if (index + 2 < words.Length)
                {
                    if (words[index+1] == "SET")
                    {
                        return words[index + 2].ToLower();
                    }
                }
            }
            return "";
        }

        public static string GetCollate(string cmd)
        {
            string[] words = cmd.ToUpper().Split(' ');
            if (words.Contains("COLLATE"))
            {
                int index = Array.IndexOf(words, "COLLATE");
                if (index + 1 < words.Length)
                {
                    return words[index + 1].ToLower();
                }
            }
            return "";
        }

        public static int GetDecimalSize(string cmd, out int d)
        {
            d = 0;
            int size = 0;
            string[] vals = Text.BracketsContent(cmd.Split(" ")[1]).Split(',');
            if (vals.Length > 1) 
            {
                int.TryParse(vals[1], out d);
                int.TryParse(vals[0], out size);
                return size;
            }
            else
            {
                d = 2;
                return 10;
            }

        }

        public static string GetDatabaseName(string cmd)
        {
            return cmd.Trim().Split(" ")[2].Replace("`", "");
        }

        public static string GetTableName(string cmd)
        {
            string[] words = cmd.Trim().Split(" ");
            if (words[1].ToUpper() == "TEMPORARY")
            {
                return words[3].Replace("`", "");
            }
            else return words[2].Replace("`", "");
        }

        public static string GetOnUpdate(string cmd)
        {
            string[] words = cmd.ToUpper().Split(' ');
            if (words.Contains("ON"))
            {
                int index = Array.IndexOf(words, "ON");
                if (index + 2 < words.Length)
                {
                    if (words[index + 1] == "UPDATE")
                    {
                        if (words[index + 2] == "RESTRICT" || words[index + 2] == "CASCADE")
                        return words[index + 2];
                    }
                }
                if (index + 3 < words.Length)
                {
                    if (words[index + 1] == "UPDATE")
                    {
                        if (words[index + 2] == "SET" || words[index + 2] == "NO")
                        {
                            return words[index + 2] + " " + words[index + 3];
                        }
                    }
                }
            }
            return "";
        }
        public static string GetOnDelete(string cmd)
        {
            string[] words = cmd.ToUpper().Split(' ');
            if (words.Contains("ON"))
            {
                int index = Array.IndexOf(words, "ON");
                if (index + 2 < words.Length)
                {
                    if (words[index + 1] == "DELETE")
                    {
                        if (words[index + 2] == "RESTRICT" || words[index + 2] == "CASCADE")
                            return words[index + 2];
                    }
                }
                if (index + 3 < words.Length)
                {
                    if (words[index + 1] == "DELETE")
                    {
                        if (words[index + 2] == "SET" || words[index + 2] == "NO")
                        {
                            return words[index + 2] + " " + words[index + 3];
                        }
                    }
                }
            }
            return "";
        }

        public static string GetFKName(string cmd)
        {
            return cmd.Trim().Split(" ")[1].Replace("`", "");
        }

        public static string GetFKRemoteTable(string cmd)
        {
            string[] words = cmd.Trim().Split(' ');
            int index = Array.IndexOf(words, "REFERENCES");
            if (index + 1 < words.Length && index != -1) 
            {
                return words[index + 1].Trim().Replace("`", "");
            }
            return "";
        }
        public static string GetUQName(string cmd)
        {
            return cmd.Trim().Split(" ")[2].Replace("`", "");
        }

        public static string GetTableValue(string cmd, string value)
        {
            string[] words = cmd.Trim().ToUpper().Split(" ");
            foreach (string s in words)
            {
                if (s.StartsWith(value + "=") && s.Length > value.Length+1)
                {
                    return s.Trim().ToLower().Split("=")[1];
                }
            }
            if (value == "AUTO_INCREMENT") return "0";
            return "";
        }
        public static bool GetTableTemporary(string cmd)
        {
            return cmd.Trim().ToUpper().Split(" ")[1] == "TEMPORARY";
        }

    }
}
