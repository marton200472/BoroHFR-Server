using BoroHFR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoroHFR.Data;
using Moq;
using Moq.EntityFrameworkCore;

namespace BoroHFR.Tests
{
    internal static class DbContextExtensions
    {
        public static void Setup(this Mock<BoroHfrDbContext> mock)
        {
            mock.Setup(x => x.Users).ReturnsDbSet(ExampleUsers);
            mock.Setup(x => x.Classes).ReturnsDbSet(ExampleClasses);
            mock.Setup(x => x.EmailConfirmationTokens).ReturnsDbSet(ExampleEmailConfirmationTokens);
            mock.Setup(x => x.InviteTokens).ReturnsDbSet(ExampleInviteTokens);
            mock.Setup(x => x.PasswordResetTokens).ReturnsDbSet(ExamplePasswordResetTokens);
            mock.Setup(x => x.Groups).ReturnsDbSet(ExampleGroups);
            mock.Setup(x => x.Subjects).ReturnsDbSet(ExampleSubjects);
            mock.Setup(x => x.Files).ReturnsDbSet(Array.Empty<Models.File>());
            mock.Setup(x => x.Events).ReturnsDbSet(ExampleEvents);
        }

        private static IEnumerable<Event> ExampleEvents => new[]
        {
            new Event()
            {
                Conversation = null,
                CreateTime = DateTime.Now,
                Creator = ExampleUsers.First(),
                Group = null,
                AssociatedUsers = new(),
                Title = "teszt",
                AttachedFiles = new()
            }
        };

        private static IEnumerable<Group> ExampleGroups =>
         new[]
            {
                new Group()
                {
                    Id = new GroupId(Guid.Parse("1b440071-93fc-4340-8c8e-878caa4cb29f")),
                    Members = new List<User>(),
                    Name = "Teszt kek",
                    Subject = ExampleSubjects.First(),
                    SubjectId = new SubjectId(Guid.Parse("634b378e-7164-4ad0-a1f1-a29d5ab1abae")),
                    Teacher = "Kek",
                    Class = ExampleClasses.First()
                },
                new Group()
                {
                    Id = new GroupId(Guid.Parse("19bae99b-79df-4c91-b010-472b214aa956")),
                    Members = new List<User>() { ExampleUsers.First() },
                    Name = "Teszt kek 3",
                    Subject = ExampleSubjects.First(),
                    SubjectId = new SubjectId(Guid.Parse("634b378e-7164-4ad0-a1f1-a29d5ab1abae")),
                    Teacher = "Kek",
                    Class = ExampleClasses.First()
                },
                new Group()
                {
                    Id = new GroupId(Guid.Parse("b245e66f-24b0-4228-9fc1-7d25b4c6ada6")),
                    Members = new List<User>(),
                    Name = "Teszt kek 2",
                    Subject = ExampleSubjects.Skip(1).First(),
                    SubjectId = new SubjectId(Guid.Parse("1b1e0989-7464-4419-85ca-3d93609c87ab")),
                    Teacher = "Kek 2",
                    Class = ExampleClasses.Skip(1).First()
                },
            };

        private static IEnumerable<Subject> ExampleSubjects =>
            new[] {
                new Subject()
                {
                    Class = ExampleClasses.First(),
                    ClassId = new ClassId(Guid.Parse("d0ef37ab-a814-4cf4-9131-192bbe021357")),
                    Id = new SubjectId(Guid.Parse("634b378e-7164-4ad0-a1f1-a29d5ab1abae")),
                    Name = "Teszt tárgy"
                },
                new Subject()
                {
                    Class = ExampleClasses.Skip(1).First(),
                    ClassId = new ClassId(Guid.Parse("059bb2b0-fce2-42c9-a109-89e1e99b4f06")),
                    Id = new SubjectId(Guid.Parse("1b1e0989-7464-4419-85ca-3d93609c87ab")),
                    Name = "Teszt tárgy 2"
                },
            };

        private static IEnumerable<Class> ExampleClasses =>
        new Class[]
            {
                new Class()
                {
                    Id = new ClassId(Guid.Parse("d0ef37ab-a814-4cf4-9131-192bbe021357")),
                    Name = "14.P"
                },
                new Class()
                {
                    Id = new ClassId(Guid.Parse("059bb2b0-fce2-42c9-a109-89e1e99b4f06")),
                    Name = "15.P"
                },
            };

        private static IEnumerable<User> ExampleUsers =>
            new User[]
            {
                new User()
                {
                    Class = ExampleClasses.First(),
                    ClassId = new ClassId(Guid.Parse("d0ef37ab-a814-4cf4-9131-192bbe021357")),
                    Role = UserRole.User,
                    Username = "test",
                    Id = new UserId(Guid.Parse("22010625-2548-4282-9fb2-a3bb4a77d2ba")),
                    EMail = "test@test.com",
                    EMailConfirmed = true,
                    PasswordHash = "$2a$11$lhibYf3PIvOcaOfvP2GcHeYSNk9X8ZvZZ4cRB9qPQc/H.rtHtOjGq" // apitest1234
                },
                new User()
                {
                    Class = ExampleClasses.First(),
                    ClassId = new ClassId(Guid.Parse("059bb2b0-fce2-42c9-a109-89e1e99b4f06")),
                    Role = UserRole.User,
                    Username = "test2",
                    Id = new UserId(Guid.Parse("81030049-bc92-479d-9a18-e2360301a653")),
                    EMail = "test2@test.com",
                    EMailConfirmed = false,
                    PasswordHash = "$2a$11$lhibYf3PIvOcaOfvP2GcHeYSNk9X8ZvZZ4cRB9qPQc/H.rtHtOjGq" // apitest1234
                },
            };


        private static IEnumerable<InviteToken> ExampleInviteTokens =>
            new[]
            {
                new InviteToken()
                {
                    Class = ExampleClasses.First(),
                    Token = "validtoken",
                    Usages = 1,
                    ValidUntil = DateTimeOffset.MaxValue
                },
                new InviteToken()
                {
                    Class = ExampleClasses.First(),
                    Token = "invalidtkn",
                    Usages = 1,
                    ValidUntil = DateTimeOffset.MinValue
                },
                new InviteToken()
                {
                    Class = ExampleClasses.First(),
                    Token = "usedtokenn",
                    Usages = 0,
                    ValidUntil = DateTimeOffset.MaxValue
                },
            };

        private static IEnumerable<PasswordResetToken> ExamplePasswordResetTokens =>
         new[]
            {
            new PasswordResetToken() {Token = new PasswordResetTokenId(Guid.Parse("cc2deed3-94c3-4cf8-91ca-f2419f8c5bd2")), User = ExampleUsers.First(), ValidUntil = DateTimeOffset.MaxValue}
        };

        private static IEnumerable<EmailConfirmationToken> ExampleEmailConfirmationTokens =>
         new[]
            {
            new EmailConfirmationToken()
            {
                Token = new EmailConfirmationTokenId(Guid.Parse("5f20ebf5-98c7-493a-91b8-6171f683c9bc")), User = ExampleUsers.First(), ValidUntil = DateTimeOffset.MaxValue
            }
         };
    }
}
