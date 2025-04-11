using Microsoft.EntityFrameworkCore;

namespace DupeDetectorWorkerService.database
{
    public class DupeDBContext : DbContext
    {
        public DupeDBContext(DbContextOptions<DupeDBContext> options)
        : base(options)
        {
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<DupeDBContext, Configuration>());
        }

        public DbSet<DuplicateFile> DuplicateFile { get; set; }
    }
}


public class DuplicateFile
{
    public DuplicateFile(string fileName, string md5CheckSum)
    {
        DuplicateFileId = new Guid();
        FileName = fileName;
        Md5CheckSum = md5CheckSum;
    }

    public Guid DuplicateFileId { get; set; }
    public string FileName { get; set; }
    public string Md5CheckSum { get; set; }
}