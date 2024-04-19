using AutoBogus;
using Bogus;
using HomeworkApp.Dal.Entities;
using HomeworkApp.IntegrationTests.Creators;

namespace HomeworkApp.IntegrationTests.Fakers;

public static class TaskCommentEntityV1Faker
{
    private static readonly object Lock = new();

    private static readonly Faker<TaskCommentEntityV1> Faker = new AutoFaker<TaskCommentEntityV1>()
        .RuleFor(x => x.Id, _ => Create.RandomId())
        .RuleFor(x => x.TaskId, _ => Create.RandomId())
        .RuleFor(x => x.AuthorUserId, _ => Create.RandomId())
        .RuleFor(x => x.At, f => f.Date.RecentOffset().UtcDateTime)
        .RuleFor(x => x.ModifiedAt, _ => null)
        .RuleFor(x => x.DeletedAt, _ => null);
    public static TaskCommentEntityV1[] Generate(int count = 1)
    {
        lock (Lock)
        {
            return Faker.Generate(count).ToArray();
        }
    }
    
    public static TaskCommentEntityV1 WithTaskId(
        this TaskCommentEntityV1 src, 
        long taskId)
        => src with { TaskId = taskId };
    
    public static TaskCommentEntityV1 WithId(
        this TaskCommentEntityV1 src, 
        long commentId)
        => src with { Id = commentId };
    
    public static TaskCommentEntityV1 WithDeletedAt(
        this TaskCommentEntityV1 src,
        DateTimeOffset deletedAt)
        => src with { DeletedAt = deletedAt};

    public static TaskCommentEntityV1 WithMessage(
        this TaskCommentEntityV1 src,
        string message)
        => src with { Message = message };
}