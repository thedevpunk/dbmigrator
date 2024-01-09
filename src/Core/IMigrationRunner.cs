namespace DevPunk.DbMigrator.Core
{
    public interface IMigrationRunner
    {
        void RunMigrations(bool isUpMigration);
    }
}
