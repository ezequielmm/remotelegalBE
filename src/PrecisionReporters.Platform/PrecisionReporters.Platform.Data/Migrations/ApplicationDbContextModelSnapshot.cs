﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrecisionReporters.Platform.Data;

namespace PrecisionReporters.Platform.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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

                    b.Property<string>("AddedById")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("CaptionId")
                        .HasColumnType("char(36)");

                    b.Property<string>("CaseId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<DateTime?>("CompleteDate")
                        .HasColumnType("datetime");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("Details")
                        .HasColumnType("text");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime");

                    b.Property<bool>("IsOnTheRecord")
                        .HasColumnType("bit");

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

                    b.HasIndex("AddedById");

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

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("DocumentId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("DepositionId");

                    b.HasIndex("DocumentId");

                    b.ToTable("DepositionDocuments");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DepositionEvent", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("Details")
                        .HasColumnType("text");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("DepositionId");

                    b.HasIndex("UserId");

                    b.ToTable("DepositionEvents");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Document", b =>
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

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("FilePath")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<long>("Size")
                        .HasColumnType("bigint");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AddedById");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DocumentUserDeposition", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("DocumentId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("DepositionId");

                    b.HasIndex("DocumentId");

                    b.HasIndex("UserId");

                    b.ToTable("DocumentUserDepositions");
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

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Role", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Roles");

                    b.HasData(
                        new
                        {
                            Id = "c7f87850-e176-4865-b26b-cedac420a0c8",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Name = "CaseAdmin"
                        },
                        new
                        {
                            Id = "6c73879b-cce3-47ea-9b80-12e1c4d1285e",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Name = "DepositionCourtReporter"
                        },
                        new
                        {
                            Id = "997d199c-3b9a-4103-a320-130b02890a5b",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Name = "DepositionAttendee"
                        },
                        new
                        {
                            Id = "ef7db7d6-4aae-11eb-b378-0242ac130002",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            Name = "DocumentOwner"
                        });
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.RolePermission", b =>
                {
                    b.Property<string>("RoleId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Action")
                        .HasColumnType("varchar(767)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.HasKey("RoleId", "Action");

                    b.ToTable("RolePermissions");

                    b.HasData(
                        new
                        {
                            RoleId = "c7f87850-e176-4865-b26b-cedac420a0c8",
                            Action = "Delete",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "c7f87850-e176-4865-b26b-cedac420a0c8",
                            Action = "Update",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "c7f87850-e176-4865-b26b-cedac420a0c8",
                            Action = "View",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "6c73879b-cce3-47ea-9b80-12e1c4d1285e",
                            Action = "EndDeposition",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "6c73879b-cce3-47ea-9b80-12e1c4d1285e",
                            Action = "Recording",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "997d199c-3b9a-4103-a320-130b02890a5b",
                            Action = "UploadDocument",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "ef7db7d6-4aae-11eb-b378-0242ac130002",
                            Action = "Delete",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "ef7db7d6-4aae-11eb-b378-0242ac130002",
                            Action = "Update",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        },
                        new
                        {
                            RoleId = "ef7db7d6-4aae-11eb-b378-0242ac130002",
                            Action = "View",
                            CreationDate = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
                        });
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

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Transcription", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("DepositionId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("TranscriptDateTime")
                        .HasColumnType("datetime");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Transcriptions");
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

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("bit");

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

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.UserResourceRole", b =>
                {
                    b.Property<string>("RoleId")
                        .HasColumnType("char(36)");

                    b.Property<string>("ResourceId")
                        .HasColumnType("char(36)");

                    b.Property<string>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreationDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime");

                    b.Property<string>("ResourceType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("RoleId", "ResourceId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("UserResourceRoles");
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
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Document", "Caption")
                        .WithMany()
                        .HasForeignKey("CaptionId");

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Case", "Case")
                        .WithMany("Depositions")
                        .HasForeignKey("CaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

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
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Deposition", "Deposition")
                        .WithMany("Documents")
                        .HasForeignKey("DepositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DepositionEvent", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Deposition", "Deposition")
                        .WithMany("Events")
                        .HasForeignKey("DepositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Document", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.DocumentUserDeposition", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Deposition", "Deposition")
                        .WithMany("DocumentUserDepositions")
                        .HasForeignKey("DepositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Document", "Document")
                        .WithMany("DocumentUserDepositions")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany("DocumentUserDepositions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.RolePermission", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Role", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.Transcription", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PrecisionReporters.Platform.Data.Entities.UserResourceRole", b =>
                {
                    b.HasOne("PrecisionReporters.Platform.Data.Entities.Role", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PrecisionReporters.Platform.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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
