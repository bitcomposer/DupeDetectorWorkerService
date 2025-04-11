using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DupeDetectorWorkerService.database
{
    public class DupeDBContext : DbContext
    {
        public DupeDBContext(DbContextOptions<DupeDBContext> options)
        : base(options)
        {
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
    [Key]
    [Required]
    public Guid DuplicateFileId { get; set; }

    [Required]
    public string FileName { get; set; }

    [Required]
    public string Md5CheckSum { get; set; }
}