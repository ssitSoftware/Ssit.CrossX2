using System.Numerics;

namespace Ssit.CrossX2.Framework.Input.Internal;

public class VelocityTracker
{
    private struct TouchMovement
    {
        public Vector2 Position;
        public double Time;
    }
    
    private readonly object _lock = new();

    private readonly Queue<TouchMovement> _touchMovements = new();
    private int _currentTouchMovement;
    
    private const int HistorySize = 20;
    
    public float CalculateTouchVelocity(int id, bool vertical)
    {
        float[] positions;
        double[] times;

        lock(_lock)
        {
            if(_currentTouchMovement != id)
            {
                return 0;
            }

            positions = vertical ? _touchMovements.Reverse().Select(o => o.Position.Y).ToArray() : 
                                   _touchMovements.Reverse().Select(o => o.Position.X).ToArray();

            times = _touchMovements.Reverse().Select(o => o.Time).ToArray();
        }

        return CalculateImpulseVelocity(times, positions);
    }
    
    public void AddTouchMovement(int id, Vector2 position, double time)
    {
        lock(_lock)
        {
            if(_currentTouchMovement == 0)
            {
                ResetImpl();
                _currentTouchMovement = id;
            }

            if(id != _currentTouchMovement)
            {
                if(_currentTouchMovement != 0)
                {
                    ResetImpl();
                }

                return;
            }

            var touchMovement = new TouchMovement
            {
                Position = position,
                Time = time
            };

            if(_touchMovements.Count > HistorySize)
            {
                _touchMovements.Dequeue();
                _touchMovements.Enqueue(touchMovement);
            }
            else
            {
                _touchMovements.Enqueue(touchMovement);
            }
        }
    }

    public void Reset()
    {
        lock(_lock)
        {
            ResetImpl();
        }
    }
    
    private void ResetImpl()
    {
        _touchMovements.Clear();
        _currentTouchMovement = 0;
    }

    private static float CalculateImpulseVelocity(double[] t, float[] x)
    {
        var count = t.Length;
    
        if (count < 2) 
        {
            return 0; // if 0 or 1 points, velocity is zero
        }
        
        if (t[1] > t[0])
        {
            return 0;
        }
    
    
        if (count == 2) 
        {
            if ( Math.Abs(t[1] - t[0]) < double.Epsilon) 
            {
                return 0;
            }
            
            float deltaX = x[1] - x[0];
            return (float)(deltaX / (t[1] - t[0]));
        }
        
        float work = 0;
        for (int i = count - 1; i > 0 ; i--) 
        { 
            if (Math.Abs(t[i] - t[i-1]) < double.Epsilon) 
            {
                continue;
            }
            
            float vprev = KineticEnergyToVelocity(work);
            float deltaX = x[i] - x[i-1];
            float vcurr = deltaX / (float)(t[i] - t[i-1]);
            
            work += (vcurr - vprev) * MathF.Abs(vcurr);
            
            if (i == count - 1)
            {
                work *= 0.5f;
            }
        }
        return KineticEnergyToVelocity(work);
    }
    
    private static float KineticEnergyToVelocity(float work) 
    {
        const float sqrt2 = 1.41421356237f;
        return (work < 0 ? -1.0f : 1.0f) * MathF.Sqrt(MathF.Min(5000000, MathF.Abs(work))) * sqrt2;
    }
}