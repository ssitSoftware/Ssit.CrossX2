namespace Ssit.CrossX2.Framework.Games.Logic;

public class GameTimer(float? minDelta, float? maxDelta): IGameTimer
{
    private float _gameTime;
    private int _framesCount;
    
    private static readonly float[] TimeDeltas =
    [
        1f / 480f, 1f / 400f, 1f/ 360f, 1f / 300f, 1f / 240f, 1f / 200f, 1f / 180f, 1f / 165f, 1f / 150f, 1f / 144f, 1f / 120f, 1f / 90f, 1f / 60f
    ];
    
    public float TimeDelta { get; private set; } = 1 / 120f;
    
    public void Update(float deltaTime)
    {
        _gameTime += deltaTime;
        _framesCount++;

        if (_gameTime >= 1)
        {
            var fps = _framesCount / _gameTime;
            
            _gameTime = 0;
            _framesCount = 0;

            var dt = 1 / fps;

            var timeDelta = TimeDeltas[0];
            for (var i = 1; i < TimeDeltas.Length; i++)
            {
                if ( MathF.Abs(dt - TimeDeltas[i]) <= MathF.Abs(dt - timeDelta) )
                {
                    timeDelta = TimeDeltas[i];
                }
            }

            var div = 1f;
            while (dt > TimeDeltas[^1] * 1.05f)
            {
                div++;
                dt = 1 / fps / div;
                timeDelta = TimeDeltas[0];
                for (var i = 1; i < TimeDeltas.Length; i++)
                {
                    if ( MathF.Abs(dt - TimeDeltas[i]) <= MathF.Abs(dt - timeDelta) )
                    {
                        timeDelta = TimeDeltas[i];
                    }
                }
            }

            if (timeDelta > maxDelta)
            {
                timeDelta = maxDelta.Value;
            }
            
            if (timeDelta < minDelta)
            {
                timeDelta = minDelta.Value;
            }


            TimeDelta = timeDelta;
        }
    }
}