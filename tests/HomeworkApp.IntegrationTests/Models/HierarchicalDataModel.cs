using HomeworkApp.Dal.Entities;

namespace HomeworkApp.IntegrationTests.Models;

public record HierarchicalDataModel
{
    public required long RootTaskId { get; init; }
    
    public required TaskEntityV1[] Tasks { get; init; }
}