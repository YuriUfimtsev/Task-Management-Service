using Xunit;
using FluentAssertions;
using HomeworkApp.Dal.Infrastructure;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.IntegrationTests.Fakers;
using HomeworkApp.IntegrationTests.Fixtures;
using Moq;

namespace HomeworkApp.IntegrationTests.RepositoryTests;

[Collection(nameof(TestFixture))]
public class TaskCommentRepositoryTests
{
    private readonly ITaskCommentRepository _repository;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderFake;

    public TaskCommentRepositoryTests(TestFixture fixture)
    {
        _repository = fixture.TaskCommentRepository;
        _dateTimeProviderFake = fixture.DateTimeProviderFake;
    }

    [Fact]
    public async Task Add_Comment_Success()
    {
        // Arrange
        var comment = TaskCommentEntityV1Faker.Generate().First();

        // Act
        var result = await _repository.Add(comment, default);
        
        // Asserts
        result.Should().BePositive();
    }

    [Fact]
    public async Task Get_CommentsWithoutDeleted_Success()
    {
        // Arrange
        var taskId = 3;
        var comments = TaskCommentEntityV1Faker
            .Generate(5)
            .Select(comment => comment.WithTaskId(taskId));
        var ids = new List<long>();
        foreach (var comment in comments)
        {
            ids.Add(await _repository.Add(comment, default));
        }

        var deletedComment = TaskCommentEntityV1Faker
            .Generate()
            .First()
            .WithDeletedAt(DateTimeOffset.UtcNow);
        ids.Add(await _repository.Add(deletedComment, default));

        // Act
        var result = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = false
            }, default);
        
        // Asserts
        result.Should().HaveCount(ids.Count - 1);
        result.Should().OnlyContain(
            taskCommentEntity => ids.Contains(taskCommentEntity.Id));
    }

    [Fact]
    public async Task SetDeleted_OnComment_Success()
    {
        // Arrange
        var taskId = 1;
        var comment = TaskCommentEntityV1Faker
            .Generate()
            .First()
            .WithTaskId(taskId);
        var commentId = await _repository.Add(comment, default);
        var deletedDate = DateTimeOffset.UtcNow;

        _dateTimeProviderFake
            .Setup(fake => fake.Now())
            .Returns(deletedDate);
        
        // Act
        await _repository.SetDeleted(commentId, default);
        
        // Asserts
        var comments = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = true
            }, default);
        
        var deletedComment = comments.First(commentEntity => commentEntity.Id == commentId);
        deletedComment.Should().NotBeNull();
        deletedComment.DeletedAt.Should().BeCloseTo(deletedDate, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Update_Comment_Success()
    {
        // Arrange
        var taskId = 2;
        var taskComment = TaskCommentEntityV1Faker
            .Generate()
            .First()
            .WithTaskId(taskId);
        var taskCommentId = await _repository.Add(taskComment, default);
        var message = "TestMessage";
        var commentToUpdate = taskComment
            .WithId(taskCommentId)
            .WithMessage(message);
        var modifiedAtDate = DateTimeOffset.UtcNow;

        _dateTimeProviderFake
            .Setup(fake => fake.Now())
            .Returns(modifiedAtDate);
        
        // Act
        await _repository.Update(commentToUpdate, default);

        // Asserts
        var comments = await _repository.Get(
            new TaskCommentGetModel()
            {
                TaskId = taskId,
                IncludeDeleted = false
            }, default);

        var updatedComment = comments.First(comment => comment.Id == taskCommentId);
        updatedComment.Should().NotBeNull();
        updatedComment.ModifiedAt.Should().BeCloseTo(modifiedAtDate, TimeSpan.FromMilliseconds(1));
        updatedComment.Message.Should().BeEquivalentTo(message);
    }
}