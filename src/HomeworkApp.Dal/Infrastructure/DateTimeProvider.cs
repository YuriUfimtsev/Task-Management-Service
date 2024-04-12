namespace HomeworkApp.Dal.Infrastructure;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now() => DateTimeOffset.Now;
}