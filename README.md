# PostgreSQL в .NET
Файл *docs/Queries.md* содержит набор сложных запросов к PostgreSQL. Выполненные задания, указанные ниже, отображают реализацию взаимодействия с PostgreSQL и Redis в .NET проекте.

## Результаты
- все методы в репозиториях покрыты интеграционными тестами;
- интеграционные тесты проверяют корректность запроса к БД и маппинга полученных результатов.

## Задание 1: Метод проверки перед завершением задания
Нужен один параметризуемый метод в репозитории, который бы прошелся рекурсивно по всем дочерним заданиям и вернул  только те подзадачи, которые находятся в указанных статусах. В ответе нужно вернуть ИД подзадачи, его заголовок, статус и путь от родительского задания к этой дочерней задаче. 

**Контракт:**
```csharp
public interface ITaskRepository
{
    ...
    Task<SubTaskModel[]> GetSubTasksInStatus(long parentTaskId, TaskStatus[] statuses, CancellationToken token);
}
...
public record SubTaskModel
{
    public required long TaskId { get; init; }
    public required string Title { get; init; }
    public required TaskStatus Status { get; init; }
    public required long[] ParentTaskIds { get; init; }
}
```

## Задание 2: Операции над сообщениями по заданию
Добавить новые поля в таблицу task_comments через миграции:
- поле `modified_at timestamp with time zone null`-  будет заполняться ТОЛЬКО при изменении сообщения
- поле `deleted_at timestamp with time zone null` - будет заполняться ТОЛЬКО при удалении сообщения

Реализовать методы pg-репозитория:

- Метод получения сообщений по заданию, отсортированных от самого позднего к самому раннему. Предусмотреть фильтр для получения в том числе удаленных заданий (по умолчанию удаленные задания не возвращаются)
- Метод добавления нового сообщения
- Метод изменения сообщения (указывает modified_at как текущую дату UTC)
- Метод отметки сообщения как удаленного (указывает deleted_at как текущую дату UTC)

**Контракты:**
```csharp
public interface ITaskCommentRepository
{
    Task<long> Add(TaskCommentEntityV1 model, CancellationToken token);
    Task Update(TaskCommentEntityV1 model, CancellationToken token);
    Task SetDeleted(long taskId, CancellationToken token);
    Task<TaskCommentEntityV1[]> Get(TaskCommentGetModel model, CancellationToken token);
}
...
public record TaskCommentGetModel
{
    public required long TaskId { get; init; }
    public required bool IncludeDeleted { get; init; }
}
```

В HomeworkApp.Bll реализовать метод в TaskService для получения сообщений по заданию, который бы использовал Redis-cache. В кэш помещать последние 5 сообщений (самые новые) и возвращать их из кэша. Время жизни кэша - 5сек.

```csharp
public interface ITaskService
{
    ...
    Task<TaskMessage[]> GetComments(long taskId, CancellationToken token);
}
...
public record TaskMessage
{
    public required long TaskId { get; init; }
    public required string Comment { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset At { get; init; }
}
```

## Задание 3: RateLimiter
На основе Redis реализовать рейт-лимитер с ограничением в 100 запросов в минуту. Можно допустить, что IP-адрес пользователя можно получить из заголовка запроса X-R256-USER-IP.
Логику ограничения пропускной способности реализовать (RateLimiterService) в отдельном bll-сервисе и встроить его в Middleware или Interceptor. Если превышен лимит, отклонять запрос с ошибкой.
Покрыть bll-сервис (RateLimiterService) с  юнит-тестами, которые бы провели основные кейсы:
- успешное выполнение запроса
- превышение лимита на выполнение
