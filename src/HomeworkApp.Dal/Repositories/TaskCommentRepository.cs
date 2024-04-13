using Dapper;
using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Infrastructure;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Microsoft.Extensions.Options;

namespace HomeworkApp.Dal.Repositories;

public class TaskCommentRepository : PgRepository, ITaskCommentRepository
{
    private readonly IDateTimeProvider _dateTimeProvider;
    
    public TaskCommentRepository(
        IOptions<DalOptions> dalSettings,
        IDateTimeProvider dateTimeProvider) : base(dalSettings.Value)
    {
        _dateTimeProvider = dateTimeProvider;
    }
    
    public async Task<long> Add(TaskCommentEntityV1 model, CancellationToken token)
    {
        const string sqlQuery = @"
insert into task_comments (task_id, author_user_id, message, at, modified_at, deleted_at) 
select task_id, author_user_id, message, at, modified_at, deleted_at
  from UNNEST(@TaskComments)
returning id;
";
        await using var connection = await GetConnection();
        var ids = await connection.QueryAsync<long>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskComments = new[] { model }
                },
                cancellationToken: token));
        
        return ids.First();
    }

    public async Task Update(TaskCommentEntityV1 model, CancellationToken token)
    {
        const string sqlQuery = @"
update task_comments
    set message = @NewMessage
      , modified_at = @ModifiedAt
  where id = @TaskCommentId;
";
        
        await using var connection = await GetConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    NewMessage = model.Message,
                    ModifiedAt = _dateTimeProvider.Now(),
                    TaskCommentId = model.Id
                },
                cancellationToken: token));
    }

    public async Task SetDeleted(long taskCommentId, CancellationToken token)
    {
        const string sqlQuery = @"
update task_comments
    set deleted_at = @DeletedAt
  where id = @TaskCommentId;
";
        
        await using var connection = await GetConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    DeletedAt = _dateTimeProvider.Now(),
                    TaskCommentId = taskCommentId
                },
                cancellationToken: token));
    }

    public async Task<TaskCommentEntityV1[]> Get(TaskCommentGetModel model, CancellationToken token)
    {
        const string sqlQuery = @"
select c.id
     , c.task_id
     , c.author_user_id
     , c.message
     , c.at
     , c.modified_at
     , c.deleted_at
  from task_comments c
 where task_id = @TaskId
         and ((@IncludeDeleted
                   and c.deleted_at is not null) or c.deleted_at is null)
 order by c.at desc;
";
        
        await using var connection = await GetConnection();
        var comments = await connection.QueryAsync<TaskCommentEntityV1>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskId = model.TaskId,
                    IncludeDeleted = model.IncludeDeleted
                },
                cancellationToken: token));
        
        return comments.ToArray();
    }
}