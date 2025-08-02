namespace Api.Extensions;

public static class ExceptionExtensions
{
    public static IResult ToErrorResult(this Exception ex, string? internalErrorMessage)
    {
        return
            ex is ArgumentException ? Results.BadRequest(ex.Message) :
            ex is InvalidOperationException ? Results.Conflict(ex.Message) :
            ex is NullReferenceException ? Results.NotFound(ex.Message) :
            Results.InternalServerError(ex.Message);
    }
}
