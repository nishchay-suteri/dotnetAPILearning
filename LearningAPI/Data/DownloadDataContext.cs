using LearningAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningAPI.Data;
public class DownloadDataContext : DbContext
{
    public DownloadDataContext(DbContextOptions<DownloadDataContext> options) : base(options)
    {

    }
    public DbSet<DownloadDataInformation> DataInformation { get; set; } = null!;
}