namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public abstract class SingleSteeringBehavior<TObject, TBehavior> : SteeringBehavior<TObject> where TBehavior : SingleSteeringBehavior<TObject, TBehavior>, new()
{
    public static readonly TBehavior Instance = new();
    
    protected SingleSteeringBehavior()
    {
        if (Instance != null)
        {
            throw new System.Exception("SingleSteeringBehavior is a singleton");
        }
    }
}