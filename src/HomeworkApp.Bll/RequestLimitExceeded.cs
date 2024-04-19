namespace HomeworkApp.Bll;

public class RequestLimitExceeded : Exception
{
    public RequestLimitExceeded()
    {
    }

    public RequestLimitExceeded(string message) : base(message)
    {
    }
}