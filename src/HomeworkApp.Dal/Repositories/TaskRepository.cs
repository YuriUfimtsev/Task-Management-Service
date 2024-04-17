using Dapper;
using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Models;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.Dal.Settings;
using Microsoft.Extensions.Options;

namespace HomeworkApp.Dal.Repositories;

public class TaskRepository : PgRepository, ITaskRepository
{
    public TaskRepository(
        IOptions<DalOptions> dalSettings) : base(dalSettings.Value)
    {
    }

    public async Task<long[]> Add(TaskEntityV1[] tasks, CancellationToken token)
    {
        const string sqlQuery = @"
insert into tasks (parent_task_id, number, title, description, status, created_at, created_by_user_id, assigned_to_user_id, completed_at) 
select parent_task_id, number, title, description, status, created_at, created_by_user_id, assigned_to_user_id, completed_at
  from UNNEST(@Tasks)
returning id;
";

        await using var connection = await GetConnection();
        var ids = await connection.QueryAsync<long>(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    Tasks = tasks
                },
                cancellationToken: token));
        
        return ids
            .ToArray();
    }

    public async Task<TaskEntityV1[]> Get(TaskGetModel query, CancellationToken token)
    {
        var baseSql = @"
select id
     , parent_task_id
     , number
     , title
     , description
     , status
     , created_at
     , created_by_user_id
     , assigned_to_user_id
     , completed_at
  from tasks
";
        
        var conditions = new List<string>();
        var @params = new DynamicParameters();

        if (query.TaskIds.Any())
        {
            conditions.Add($"id = ANY(@TaskIds)");
            @params.Add($"TaskIds", query.TaskIds);
        }
        
        var cmd = new CommandDefinition(
            baseSql + $" WHERE {string.Join(" AND ", conditions)} ",
            @params,
            commandTimeout: DefaultTimeoutInSeconds,
            cancellationToken: token);
        
        await using var connection = await GetConnection();
        return (await connection.QueryAsync<TaskEntityV1>(cmd))
            .ToArray();
    }

    public async Task Assign(AssignTaskModel model, CancellationToken token)
    {
        const string sqlQuery = @"
update tasks
   set assigned_to_user_id = @AssignToUserId
     , status = @Status
 where id = @TaskId
";

        await using var connection = await GetConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlQuery,
                new
                {
                    TaskId = model.TaskId,
                    AssignToUserId = model.AssignToUserId,
                    Status = model.Status
                },
                cancellationToken: token));
    }

    public async Task<SubTaskModel[]> GetSubTasksInStatus(
        long parentTaskId, Dal.Enums.TaskStatus[] statuses, CancellationToken token)
    {
        const string baseSqlQuery = @"
with recursive subtasks
       as (select t.id
                , t.title
                , t.status
                , t.parent_task_id
                , array[t.id] as path_array
             from tasks t
            where id = @ParentTaskId
            union all
           select t.id
                , t.title
                , t.status
                , t.parent_task_id
                , s.path_array || t.id
             from subtasks s
             join tasks t on t.parent_task_id = s.id)
select s.id
     , s.title
     , s.status
     , s.path_array
  from subtasks s
 where s.status = any(@Statuses)
";
        await using var connection = await GetConnection();
        var cmd = new CommandDefinition(
            baseSqlQuery + " AND Id <> @ParentTaskId",
            new
            {
                ParentTaskId = parentTaskId,
                Statuses = statuses.Select(st => (int)st).ToArray()
            },
            cancellationToken: token);
        
        var result = await connection.QueryAsync(cmd);
        var subtasks = result.Select(row => 
            new SubTaskModel
            {
                TaskId = row.id,
                Title = row.title,
                Status = (Dal.Enums.TaskStatus)row.status,
                ParentTaskIds = row.path_array as long[] ?? Array.Empty<long>()
            }).ToArray();
        
        return subtasks;
    }
}