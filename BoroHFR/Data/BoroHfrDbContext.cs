using BoroHFR.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.VisualBasic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BoroHFR.Data
{
    public class BoroHfrDbContext : DbContext, IDataProtectionKeyContext
    {
        public BoroHfrDbContext(DbContextOptions o) : base(o)
        {}
        
        public BoroHfrDbContext()
        {}


        public virtual DbSet<Class> Classes { get; set; } = null!;
        public virtual DbSet<Conversation> Conversations { get; set; } = null!;
        public virtual DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;
        public virtual DbSet<Models.File> Files { get; set; } = null!;
        public virtual DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; } = null!;
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<DbLesson> Lessons { get; set; } = null!;
        public virtual DbSet<Event> Events { get; set; } = null!;
        public virtual DbSet<InviteToken> InviteTokens { get; set; } = null!;
        public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public virtual DbSet<Subject> Subjects { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        
        public virtual DbSet<Email> EmailQueue { get; set; } = null!;

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(x => x.Groups).WithMany(x => x.Members).UsingEntity(x=>x.ToTable("GroupMemberships"));
            modelBuilder.Entity<User>().HasMany(x => x.AssociatedEvents).WithMany(x => x.AssociatedUsers).UsingEntity(x => x.ToTable("AssociatedUsers"));

            modelBuilder.Entity<Email>().Property<int>("Id");
            modelBuilder.Entity<Email>().HasKey("Id");
            modelBuilder.Entity<Email>().Property(x=>x.Recipients).HasConversion(x=>JsonSerializer.Serialize(x,JsonSerializerOptions.Default), y => JsonSerializer.Deserialize<string[]>(y, JsonSerializerOptions.Default)!);

            modelBuilder.Entity<Event>().HasOne(x => x.Creator);
            modelBuilder.Entity<Event>().HasOne(x => x.Modifier);
            modelBuilder.Entity<Event>().HasMany(x => x.AttachedFiles).WithMany().UsingEntity(c => c.ToTable("EventAttachedFiles"));

            modelBuilder.Entity<Class>().HasMany(x => x.Members).WithOne(y => y.Class);


            modelBuilder.Entity<Group>().HasOne(x => x.Class).WithMany(y => y.Groups);

            modelBuilder.Entity<Subject>().HasMany(x => x.Groups).WithOne(x => x.Subject).IsRequired(false);

            modelBuilder.Entity<ConversationMessage>().HasMany(x => x.Attachments).WithMany().UsingEntity(c=>c.ToTable("ChatAttachments"));


            //strongly typed id converters
            modelBuilder.Entity<Event>().Property(x=>x.Id).HasConversion(id => id.value, guid => new Models.EventId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<DbLesson>().Property(x=>x.Id).HasConversion(id => id.value, guid => new DbLessonId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<Class>().Property(x=>x.Id).HasConversion(id => id.value, guid => new ClassId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<Conversation>().Property(x => x.Id).HasConversion(id => id.value, guid => new ConversationId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<ConversationMessage>().Property(x => x.Id).HasConversion(id => id.value, guid => new ConversationMessageId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<EmailConfirmationToken>().Property(x => x.Token).HasConversion(id => id.value, guid => new EmailConfirmationTokenId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<Group>().Property(x => x.Id).HasConversion(id => id.value, guid => new GroupId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<InviteToken>().Property(x => x.Id).HasConversion(id => id.value, guid => new InviteTokenId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<Models.File>().Property(x => x.Id).HasConversion(id => id.value, guid => new FileId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<PasswordResetToken>().Property(x => x.Token).HasConversion(id => id.value, guid => new PasswordResetTokenId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<Subject>().Property(x => x.Id).HasConversion(id => id.value, guid => new SubjectId(guid)).ValueGeneratedOnAdd();
            modelBuilder.Entity<User>().Property(x => x.Id).HasConversion(id => id.value, guid => new UserId(guid)).ValueGeneratedOnAdd();


            modelBuilder.Entity<Class>().Property(x => x.DefaultGroupId).HasConversion(id => id.value, guid => new GroupId(guid));
            modelBuilder.Entity<Group>().Property(x => x.DefaultConversationId).HasConversion(id => id.value, guid => new ConversationId(guid));


            modelBuilder.Entity<Conversation>().Property(x => x.GroupId).HasConversion(id => id.value, guid => new GroupId(guid));

            modelBuilder.Entity<ConversationMessage>().Property(x => x.ConversationId).HasConversion(id => id.value, guid => new ConversationId(guid));
            modelBuilder.Entity<ConversationMessage>().Property(x => x.SenderId).HasConversion(id => id.value, guid => new UserId(guid));

            modelBuilder.Entity<DbLesson>().Property(x => x.GroupId).HasConversion(id => id.value, guid => new GroupId(guid));

            modelBuilder.Entity<EmailConfirmationToken>().Property(x => x.UserId).HasConversion(id => id.value, guid => new UserId(guid));

            modelBuilder.Entity<Event>().Property(x => x.GroupId).HasConversion(id => id.value, guid => new GroupId(guid));
            modelBuilder.Entity<Event>().Property(x => x.ConversationId).HasConversion(id => id.value, guid => new ConversationId(guid));
            modelBuilder.Entity<Event>().Property(x => x.CreatorId).HasConversion(id => id.value, guid => new UserId(guid));
            modelBuilder.Entity<Event>().Property(x => x.ModifierId).HasConversion<Guid?>(id => !id.HasValue ? null : id.Value.value, guid => !guid.HasValue ? null :new UserId(guid.Value));
            


            modelBuilder.Entity<Group>().Property(x => x.SubjectId).HasConversion<Guid?>(id => !id.HasValue ? null : id.Value.value, guid => !guid.HasValue ? null : new SubjectId(guid.Value));
            modelBuilder.Entity<Group>().Property(x => x.ClassId).HasConversion(id => id.value, guid => new ClassId(guid));

            modelBuilder.Entity<InviteToken>().Property(x => x.ClassId).HasConversion(id => id.value, guid => new ClassId(guid));

            modelBuilder.Entity<PasswordResetToken>().Property(x => x.UserId).HasConversion(id => id.value, guid => new UserId(guid));

            modelBuilder.Entity<Subject>().Property(x => x.ClassId).HasConversion(id => id.value, guid => new ClassId(guid));

            modelBuilder.Entity<User>().Property(x => x.ClassId).HasConversion(id => id.value, guid => new ClassId(guid));

            modelBuilder.Entity<Models.File>().Property(x => x.OwnerId).HasConversion(id => id.value, guid => new UserId(guid));
        }
    }
}
