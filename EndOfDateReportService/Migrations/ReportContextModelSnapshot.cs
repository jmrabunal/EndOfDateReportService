﻿// <auto-generated />
using System;
using EndOfDateReportService.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EndOfDateReportService.Migrations
{
    [DbContext(typeof(ReportContext))]
    partial class ReportContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.21")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("EndOfDateReportService.Domain.Branch", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Branches");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Moore Wilsons Wellington"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Moore Wilsons Porirua"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Moore Wilsons Lower Hutt"
                        },
                        new
                        {
                            Id = 4,
                            Name = "Moore Wilsons Masterton"
                        });
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.Lane", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("BranchId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BranchId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Lanes");
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.PaymentMethod", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("ActualAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("BranchId")
                        .HasColumnType("int");

                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("LaneId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ReportDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("ReportedAmount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("TotalVariance")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Name");

                    b.HasIndex("LaneId");

                    b.HasIndex("Name", "LaneId", "BranchId", "ReportDate")
                        .IsUnique();

                    b.ToTable("PaymentMethods");
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.Lane", b =>
                {
                    b.HasOne("EndOfDateReportService.Domain.Branch", "Branch")
                        .WithMany("Lanes")
                        .HasForeignKey("BranchId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Branch");
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.PaymentMethod", b =>
                {
                    b.HasOne("EndOfDateReportService.Domain.Lane", "Lane")
                        .WithMany("PaymentMethods")
                        .HasForeignKey("LaneId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Lane");
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.Branch", b =>
                {
                    b.Navigation("Lanes");
                });

            modelBuilder.Entity("EndOfDateReportService.Domain.Lane", b =>
                {
                    b.Navigation("PaymentMethods");
                });
#pragma warning restore 612, 618
        }
    }
}
