namespace DBDesignerWIP
{
    public static class DataStore
    {
        public static List<Database> databases = new List<Database>();
        public static bool databasesLoaded = false;

        public static Database activeDatabase = null;

        public static Table activeTable = null;

        public static SqlDb dbConnection = null;

        public static List<string> batch = new List<string>();
        public static List<string> batchDeNovo = new List<string>();
    }
}
