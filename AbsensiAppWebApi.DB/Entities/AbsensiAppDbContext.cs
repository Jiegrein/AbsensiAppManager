using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class AbsensiAppDbContext : DbContext
    {
        public AbsensiAppDbContext(DbContextOptions<AbsensiAppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Blob> Blobs { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<ScanEnum> ScanEnums { get; set; }
        public virtual DbSet<Worker> Workers { get; set; }
        public virtual DbSet<WorkerLog> WorkerLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "English_Indonesia.1252");

            modelBuilder.Entity<Blob>(entity =>
            {
                entity.Property(e => e.BlobId).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(e => e.ProjectId).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Blob)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.BlobId)
                    .HasConstraintName("fk_project__blob");
            });

            modelBuilder.Entity<ScanEnum>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Worker>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<WorkerLog>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("pk_worker_log");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Worker)
                    .WithMany(p => p.WorkerLogs)
                    .HasForeignKey(d => d.WorkerId)
                    .HasConstraintName("fk_worker_log__worker");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
