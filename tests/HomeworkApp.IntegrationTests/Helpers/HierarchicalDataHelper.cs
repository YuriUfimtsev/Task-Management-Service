using HomeworkApp.Dal.Entities;
using HomeworkApp.Dal.Repositories.Interfaces;
using HomeworkApp.IntegrationTests.Fakers;
using HomeworkApp.IntegrationTests.Models;

namespace HomeworkApp.IntegrationTests.Helpers;

public static class HierarchicalDataHelper
{
    public static HierarchicalDataModel Generate(
        Dal.Enums.TaskStatus essentialStatus = Dal.Enums.TaskStatus.Canceled,
        int subtasksOnTask = 5)
    {
        var allTasks = new List<TaskEntityV1>();
        
        var rootTask = TaskEntityV1Faker.Generate().First().WithStatus(essentialStatus);
        allTasks.Add(rootTask);
        
        var rootSubtasks = TaskEntityV1Faker.Generate(subtasksOnTask)
            .Select(subtask => subtask.WithParentTaskId(rootTask.Id))
            .ToArray();
        rootSubtasks[0] = rootSubtasks[0].WithStatus(essentialStatus);
        allTasks.AddRange(rootSubtasks);

        foreach (var task in rootSubtasks)
        {
            var parentId = task.Id;
            var subtasks = TaskEntityV1Faker.Generate(subtasksOnTask)
                .Select(subtask => subtask.WithParentTaskId(parentId)).ToArray();
            subtasks[0] = subtasks[0].WithStatus(essentialStatus); 
            allTasks.AddRange(subtasks);
        }

        return new HierarchicalDataModel
        {
            RootTaskId = rootTask.Id,
            Tasks = allTasks.ToArray()
        };
    }

    public static async Task<HierarchicalDataModel> Fill(ITaskRepository taskRepository, HierarchicalDataModel dataModel)
    {
        var idMapper = new Dictionary<long, long>();

        var resultTasks = new List<TaskEntityV1>();
        foreach (var task in dataModel.Tasks)
        {
            var actualTask = task;
            if (task.ParentTaskId.HasValue && idMapper.ContainsKey(task.ParentTaskId.Value))
            {
                actualTask = task.WithParentTaskId(idMapper[task.ParentTaskId.Value]);
            }
            
            var newId = (await taskRepository.Add(new[] { actualTask }, default))
                .First();
            idMapper.Add(task.Id, newId);
            resultTasks.Add(task.WithId(newId).WithParentTaskId(actualTask.ParentTaskId));
        }

        return new HierarchicalDataModel()
        {
            RootTaskId = idMapper[dataModel.RootTaskId],
            Tasks = resultTasks.ToArray()
        };
    }
}