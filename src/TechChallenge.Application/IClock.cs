namespace TechChallenge.Application;

internal interface IClock
{
    DateTime Now();
}

internal class Clock : IClock
{
    public DateTime Now() => DateTime.UtcNow;
}
