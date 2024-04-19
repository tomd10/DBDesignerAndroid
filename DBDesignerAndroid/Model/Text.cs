using System.Globalization;

namespace DBDesignerWIP
{
    public static class Text
    {
        public static string ToBracket(string s)
        {
            int x = s.IndexOf('(');
            if (x == -1) return s;
            return s.Substring(0, x);
        }

        public static string BracketsContent(string s)
        {
            int x = s.IndexOf("(");
            int y = s.IndexOf(")");
            if (x == -1 || y == -1) return s;
            if (y < x) return s;
            return s.Substring(x+1,y-x-1);
        }

        public static string IntegerLiteralAfter(string s, string word)
        {
            string[] words = s.Split(' ');
            List<int> indexes = AllIndexesOfArray(words, word);
            string output = "";
            long x = 0;
            foreach (int i in indexes)
            {
                if (i + 1 < words.Length && words[i+1].Trim().ToUpper() == "NULL")
                {
                    return "NULL";
                }
                if (i+1 < words.Length && long.TryParse(words[i+1], out x))
                {
                    output = x.ToString();
                    return output;
                }
            }

            return "";
        }

        public static string DecimalLiteralAfter(string s, string word)
        {
            string[] words = s.Split(' ');
            List<int> indexes = AllIndexesOfArray(words, word);
            string output = "";
            double x = 0;
            foreach (int i in indexes)
            {
                if (i + 1 < words.Length && words[i + 1].Trim().ToUpper() == "NULL")
                {
                    return "NULL";
                }
                if (i + 1 < words.Length && double.TryParse(words[i + 1].Replace(',', '.'), CultureInfo.InvariantCulture, out x))
                {
                    output = x.ToString().Replace(',', '.');
                    return output;
                }
            }

            return "";
        }

        public static string QuotedStringAfter(string s, string word)
        {
            List<int> indexes = AllIndexesOf(s, word);
            string output = "";
            foreach (int i in indexes)
            {
                int index = i + word.Length;
                if (s[index] == '\'')
                {
                    for (int j = index + 1; j < s.Length; j++)
                    {
                        if (s[j] != '\'')
                        {
                            output += s[j];
                        }
                        else return output;
                    }
                }
            }

            return output;
        }

        public static string BracketedStringAfter(string s, string word)
        {
            List<int> indexes = AllIndexesOf(s, word);
            string output = "";
            foreach (int i in indexes)
            {
                int index = i + word.Length;
                if (s[index] == '(')
                {
                    for (int j = index + 1; j < s.Length; j++)
                    {
                        if (s[j] != ')')
                        {
                            output += s[j];
                        }
                        else return output;
                    }
                }
            }

            return output;
        }

        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        public static List<int> AllIndexesOfArray(string[] str, string value)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == value) indexes.Add(i);
            }
            return indexes;

        }

        public static string GetBracket(string s, int n)
        {
            List<int> indexes = AllIndexesOf(s, "(");
            if (indexes.Count < n + 1) return "";
            string result = "";
            for (int i = indexes[n]+1; i <s.Length; i++)
            {
                if (s[i] == ')') return result;
                result = result + s[i];
            }
            return result;
        }

        public static List<string> GetUnwrappedBracket(string s, int n)
        {
            string bracket = GetBracket(s, n);
            List<string> result = new List<string>();
            string[] words = bracket.Split(",");
            foreach(string str in words)
            {
                result.Add(str.Trim().Replace("`", ""));
            }

            return result;
        }

        public static List<string> GetColumnDefs(string cmd)
        {
            List<string> defs = new List<string>();
            string cmdTrim = cmd.Trim().Replace("\r", "\n").Replace("\n\n", "\n");
            string[] words = cmdTrim.Split("\n");
            for (int i = 0; i <words.Length; i++)
            {
                words[i] = words[i].Trim();
                if (words[i].EndsWith(",")) words[i] = words[i].Substring(0, words[i].Length - 1);
                if (words[i].StartsWith("`")) defs.Add(words[i]);
            }
            return defs;
        }

        public static List<string> GetConstraintDefs(string cmd)
        {
            List<string> defs = new List<string>();
            string cmdTrim = cmd.Trim().Replace("\r", "\n").Replace("\n\n", "\n");
            string[] words = cmdTrim.Split("\n");
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].Trim();
                //if (words[i].StartsWith("KEY") /* || words[i].StartWith("whatever" */) continue;
                if (words[i].EndsWith(",")) words[i] = words[i].Substring(0, words[i].Length - 1);
                if (!words[i].StartsWith("`") && i != 0 && i != words.Length - 1) defs.Add(words[i]);
            }
            return defs;
        }

        public static string GetColumnType(string s)
        {
            string type = s.Trim().ToUpper().Split(" ")[1];
            return type.Split("(")[0].Trim();
        }

        public static string GetConstraintType(string s)
        {
            return s.Trim().ToUpper().Split(" ")[0].Trim();
        }
    }
}
