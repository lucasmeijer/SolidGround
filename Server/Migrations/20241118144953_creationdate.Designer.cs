﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SolidGround;

#nullable disable

namespace SolidGround.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20241118144953_creationdate")]
    partial class creationdate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0-rc.2.24474.1");

            modelBuilder.Entity("InputTag", b =>
                {
                    b.Property<int>("InputsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("InputsId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("InputSetInputs", (string)null);
                });

            modelBuilder.Entity("SolidGround.Execution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsReference")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<bool>("SolidGroundInitiated")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Executions");
                });

            modelBuilder.Entity("SolidGround.Input", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalRequest_Body")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalRequest_ContentType")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalRequest_Method")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalRequest_QueryString")
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalRequest_Route")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Inputs");
                });

            modelBuilder.Entity("SolidGround.InputFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Bytes")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InputId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MimeType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("InputId");

                    b.ToTable("InputFiles");
                });

            modelBuilder.Entity("SolidGround.InputString", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InputId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("InputId");

                    b.ToTable("InputStrings");
                });

            modelBuilder.Entity("SolidGround.Output", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal?>("Cost")
                        .HasColumnType("TEXT");

                    b.Property<int>("ExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InputId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ExecutionId");

                    b.HasIndex("InputId");

                    b.ToTable("Outputs");
                });

            modelBuilder.Entity("SolidGround.OutputComponent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int>("OutputId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OutputId");

                    b.ToTable("OutputComponents");
                });

            modelBuilder.Entity("SolidGround.StringVariable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int?>("OutputId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ExecutionId");

                    b.HasIndex("OutputId");

                    b.ToTable("StringVariable");
                });

            modelBuilder.Entity("SolidGround.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("InputTag", b =>
                {
                    b.HasOne("SolidGround.Input", null)
                        .WithMany()
                        .HasForeignKey("InputsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SolidGround.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SolidGround.InputFile", b =>
                {
                    b.HasOne("SolidGround.Input", null)
                        .WithMany("Files")
                        .HasForeignKey("InputId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SolidGround.InputString", b =>
                {
                    b.HasOne("SolidGround.Input", "Input")
                        .WithMany("Strings")
                        .HasForeignKey("InputId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Input");
                });

            modelBuilder.Entity("SolidGround.Output", b =>
                {
                    b.HasOne("SolidGround.Execution", "Execution")
                        .WithMany("Outputs")
                        .HasForeignKey("ExecutionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SolidGround.Input", "Input")
                        .WithMany("Outputs")
                        .HasForeignKey("InputId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Execution");

                    b.Navigation("Input");
                });

            modelBuilder.Entity("SolidGround.OutputComponent", b =>
                {
                    b.HasOne("SolidGround.Output", "Output")
                        .WithMany("Components")
                        .HasForeignKey("OutputId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Output");
                });

            modelBuilder.Entity("SolidGround.StringVariable", b =>
                {
                    b.HasOne("SolidGround.Execution", "Execution")
                        .WithMany("StringVariables")
                        .HasForeignKey("ExecutionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SolidGround.Output", "Output")
                        .WithMany("StringVariables")
                        .HasForeignKey("OutputId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Execution");

                    b.Navigation("Output");
                });

            modelBuilder.Entity("SolidGround.Execution", b =>
                {
                    b.Navigation("Outputs");

                    b.Navigation("StringVariables");
                });

            modelBuilder.Entity("SolidGround.Input", b =>
                {
                    b.Navigation("Files");

                    b.Navigation("Outputs");

                    b.Navigation("Strings");
                });

            modelBuilder.Entity("SolidGround.Output", b =>
                {
                    b.Navigation("Components");

                    b.Navigation("StringVariables");
                });
#pragma warning restore 612, 618
        }
    }
}
