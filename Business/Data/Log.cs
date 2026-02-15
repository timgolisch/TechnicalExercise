using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    //DevNote: Talk with SWA about using microsoft.extensions.logging or serilog instead of putting logs into the DB

    [Table("Log")]
    public class Log
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        
        public string StackTrace { get; set; } = string.Empty;

        public DateTime EventDateTime { get; set; } = DateTime.UtcNow;

        public Log() { }

        public Log(string title, string description)
        {
            Type = "Trace";
            Title = title;
            Description = description;
        }
        public Log(string type, string title, string description)
        {
            Type = type;
            Title = title;
            Description = description;
        }
        public Log(Exception ex)
        {
            Type = "Exception";
            Title = ex.Message;
            Description = ex.Source;
            StackTrace = ex.StackTrace;
        }
    }

    public class LogConfiguration : IEntityTypeConfiguration<Log>
    {
        public void Configure(EntityTypeBuilder<Log> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.EventDateTime);
            builder.Property<DateTime>(x => x.EventDateTime).ValueGeneratedOnAdd();
        }
    }
}
