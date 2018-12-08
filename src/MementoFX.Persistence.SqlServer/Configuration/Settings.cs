namespace MementoFX.Persistence.SqlServer.Configuration
{
    public class Settings
    {
        public Settings(string connectionString, bool autoIncrementalTableMigrations = true, bool useCompression = false, bool useSingleTable = false)
        {
            this.ConnectionString = connectionString;
            this.AutoIncrementalTableMigrations = autoIncrementalTableMigrations;
            this.UseCompression = useCompression;
            this.UseSingleTable = useSingleTable;
        }

        public string ConnectionString { get; }

        public bool AutoIncrementalTableMigrations { get; }

        public bool UseCompression { get; }

        public bool UseSingleTable { get; }
    }
}
