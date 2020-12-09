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
    [Migration("20201202151054_AddDepositionStatus")]
    partial class AddDepositionStatus
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Case", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("AddedById")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("CaseNumber")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AddedById");

                    b.ToTable("Cases");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Composition", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime");

                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("datetime");

                    b.Property<string>("MediaUri")
                        .HasColumnType("text");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("SId")
                        .HasColumnType("text");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoomId")
                        .IsUnique();

                    b.ToTable("Compositions");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Deposition", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("CaptionId")
                        .HasColumnType("char(36)");

                    b.Property<string>("CaseId")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("Details")
                        .HasColumnType("text");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsVideoRecordingNeeded")
                        .HasColumnType("bit");

                    b.Property<string>("RequesterId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("RoomId")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("WitnessId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("CaptionId");

                    b.HasIndex("CaseId");

                    b.HasIndex("RequesterId");

                    b.HasIndex("RoomId");

                    b.HasIndex("WitnessId");

                    b.ToTable("Depositions");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DepositionDocument", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("AddedById")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .HasColumnType("char(36)");

                    b.Property<string>("FilePath")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AddedById");

                    b.HasIndex("DepositionId");

                    b.ToTable("DepositionDocuments");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Member", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("CaseId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("CaseId");

                    b.HasIndex("UserId");

                    b.ToTable("Members");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Participant", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Email")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Phone")
                        .HasColumnType("text");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("DepositionId");

                    b.HasIndex("UserId");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Room", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsRecordingEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(255)")
                        .HasMaxLength(255);

                    b.Property<string>("SId")
                        .HasColumnType("text");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Rooms");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.User", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("CompanyAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("CompanyName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasColumnType("varchar(255)")
                        .HasMaxLength(255);

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("EmailAddress")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.VerifyUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("bit");

                    b.Property<string>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("VerifyUsers");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Case", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Composition", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Room", "Room")
                        .WithOne("Composition")
                        .HasForeignKey("PrecisionReporters.Platform.Data.Entities.Composition", "RoomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Deposition", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.DepositionDocument", "Caption")
                        .WithMany()
                        .HasForeignKey("CaptionId");

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Case", null)
                        .WithMany("Depositions")
                        .HasForeignKey("CaseId");

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "Requester")
                        .WithMany()
                        .HasForeignKey("RequesterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Room", "Room")
                        .WithMany()
                        .HasForeignKey("RoomId");

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Participant", "Witness")
                        .WithMany()
                        .HasForeignKey("WitnessId");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DepositionDocument", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Deposition", null)
                        .WithMany("Documents")
                        .HasForeignKey("DepositionId");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Member", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Case", "Case")
                        .WithMany("Members")
                        .HasForeignKey("CaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany("MemberOn")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Participant", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Deposition", null)
                        .WithMany("Participants")
                        .HasForeignKey("DepositionId");

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.VerifyUser", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });
#pragma warning restore 612, 618
        }
    }
}