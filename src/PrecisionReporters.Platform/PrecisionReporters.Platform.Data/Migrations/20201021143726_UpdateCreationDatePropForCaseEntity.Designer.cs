﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrecisionReporters.Platform.Data;

namespace PrecisionReporters.Platform.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20201021143726_UpdateCreationDatePropForCaseEntity")]
    partial class UpdateCreationDatePropForCaseEntity
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Case", b =>
                {
                    b.Property<byte[]>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varbinary(16)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Cases");
                });
#pragma warning restore 612, 618
        }
    }
}
