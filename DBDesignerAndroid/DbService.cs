namespace DBDesignerWIP
{
    public interface IScopedService
    {

    }
    public class DbService : IScopedService
    {
        public List<Database> databases = new List<Database>();
        public bool databasesLoaded = false;

        public Database activeDatabase = null;

        public Table activeTable = null;

        public SqlDb dbConnection = null;

        public List<string> batch = new List<string>();
        public List<string> batchDeNovo = new List<string>();    
        public string ShowColumn()
        {
            //= (ctx.activeDatabase != null && ctx.activeDatabase.tables.Count > 0) ? Choices.GetTableNames(ctx)[0] : "";
            if (activeDatabase != null && activeDatabase.tables.Count > 0)
            {
                return Choices.GetTableNames(this)[0];
            }
            else return "";
        }

        public Column GetColumn()
        {
            return activeTable.columns[0];
        }
    }


}
