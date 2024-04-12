namespace HomeworkApp.Dal.Infrastructure;

public interface IDateTimeProvider
{
    public DateTimeOffset Now();
}