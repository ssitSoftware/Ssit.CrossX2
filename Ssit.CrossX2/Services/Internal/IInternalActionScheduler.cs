namespace CrossX2.Services.Internal;

public interface IInternalActionScheduler: IActionScheduler
{
    void Process();
}