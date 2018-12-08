namespace MementoFX.Persistence.SqlServer.Tests
{
    internal static class Config
    {
        public const string ConnectionString = @"Server=.\SQLEXPRESS;Initial Catalog=MementoFXTests;Integrated Security=true";
        public const string CompressionConnectionString = @"Server=.\SQLEXPRESS;Initial Catalog=MementoFXTests-Compression;Integrated Security=true";
        public const string SingleTableConnectionString = @"Server=.\SQLEXPRESS;Initial Catalog=MementoFXTests-SingleTable;Integrated Security=true";
    }
}
