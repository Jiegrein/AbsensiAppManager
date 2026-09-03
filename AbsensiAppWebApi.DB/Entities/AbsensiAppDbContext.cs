using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
            modelBuilder.Entity<Blob>(entity =>
            {
                entity.ToTable("blob");

                entity.Property(e => e.BlobId)
                    .ValueGeneratedNever()
                    .HasColumnName("blob_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("created_by");

                entity.Property(e => e.FileName).HasColumnName("file_name");

                entity.Property(e => e.Path).HasColumnName("path");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("project");

                entity.Property(e => e.ProjectId)
                    .ValueGeneratedNever()
                    .HasColumnName("project_id");

                entity.Property(e => e.BlobId).HasColumnName("blob_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("created_by");

                entity.Property(e => e.HourOffsetGmt)
                    .HasColumnName("hour_offset_gmt")
                    .HasDefaultValueSql("7");

                entity.Property(e => e.ProjectName).HasColumnName("project_name");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.HasOne(d => d.Blob)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.BlobId)
                    .HasConstraintName("fk_project__blob");
            });

            modelBuilder.Entity<ScanEnum>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("scan_enum");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("created_by");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name).HasColumnName("name");
            });

            modelBuilder.Entity<Worker>(entity =>
            {
                entity.ToTable("worker");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("id");

                entity.Property(e => e.BreakStatus).HasColumnName("break_status");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.DailyPay).HasColumnName("daily_pay");

                entity.Property(e => e.Fullname).HasColumnName("fullname");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.Phone).HasColumnName("phone");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.WorkStatus).HasColumnName("work_status");
            });

            modelBuilder.Entity<WorkerLog>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("pk_worker_log");

                entity.ToTable("worker_log");

                entity.Property(e => e.LogId).HasColumnName("log_id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasColumnName("created_by");

                entity.Property(e => e.EndBreak).HasColumnName("end_break");

                entity.Property(e => e.EndWork).HasColumnName("end_work");

                entity.Property(e => e.ProjectId).HasColumnName("project_id");

                entity.Property(e => e.StartBreak).HasColumnName("start_break");

                entity.Property(e => e.StartWork).HasColumnName("start_work");

                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

                entity.Property(e => e.WorkerId).HasColumnName("worker_id");

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
