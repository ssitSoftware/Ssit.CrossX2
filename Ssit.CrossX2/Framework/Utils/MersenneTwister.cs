namespace Ssit.CrossX2.Framework.Utils;

public class MersenneTwister
{
    private static readonly int N = 624;
    private static readonly int M = 397;
    private readonly UInt32 _matrixA = 0x9908b0df;
    private readonly UInt32 _upperMask = 0x80000000;
    private readonly UInt32 _lowerMask = 0x7fffffff;
    private readonly UInt32 _temperingMaskB = 0x9d2c5680;
    private readonly UInt32 _temperingMaskC = 0xefc60000;
    private readonly double _finalConstant = 2.3283064365386963e-10;

    private static readonly object Locker = new();
    
    public static MersenneTwister Shared
    {
        get
        {
            if (field == null)
            {
                lock (Locker)
                {
                    if (field == null)
                    {
                        field = new ((ulong)DateTime.Now.Ticks);
                    }
                }
            }
            return field;
        }
    }
    
    public MersenneTwister(ulong seed = 0)
    {
        Sgenrand(seed + 4327);
    }
    
    private ulong TEMPERING_SHIFT_U(ulong y)
    {
        return y >> 11;
    }

    private ulong TEMPERING_SHIFT_S(ulong y)
    {
        return y << 7;
    }

    private ulong TEMPERING_SHIFT_T(ulong y)
    {
        return y << 15;
    }

    private ulong TEMPERING_SHIFT_L(ulong y)
    {
        return y >> 18;
    }

    /// <summary>
    /// the array for the state vector
    /// </summary>
    private readonly ulong[] _mt = new ulong[625];

    /// <summary>
    /// mti==N+1 means mt[N] is not initialized 
    /// </summary>
    private int _mti = N + 1;
    
    /// <summary>
    /// setting initial seeds to mt[N] using
    /// the generator Line 25 of Table 1 in
    /// [KNUTH 1981, The Art of Computer Programming Vol. 2 (2nd Ed.), pp102] 
    /// </summary>
    /// <param name="seed"></param>
    private void Sgenrand(ulong seed)
    {
        _mt[0] = seed & 0xffffffff;
        for (_mti = 1; _mti < N; _mti++)
            _mt[_mti] = (69069 * _mt[_mti - 1]) & 0xffffffff;
    }

    private double Genrand()
    {
        ulong y;
        ulong[] mag01 = new ulong[2] { 0x0, _matrixA };
        /* mag01[x] = x * MATRIX_A  for x=0,1 */

        if (_mti >= N)
        { /* generate N words at one time */
            int kk;

            if (_mti == N + 1)   /* if sgenrand() has not been called, */
                Sgenrand(4357); /* a default initial seed is used   */

            for (kk = 0; kk < N - M; kk++)
            {
                y = (_mt[kk] & _upperMask) | (_mt[kk + 1] & _lowerMask);
                _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1];
            }
            for (; kk < N - 1; kk++)
            {
                y = (_mt[kk] & _upperMask) | (_mt[kk + 1] & _lowerMask);
                _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1];
            }
            y = (_mt[N - 1] & _upperMask) | (_mt[0] & _lowerMask);
            _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1];

            _mti = 0;
        }

        y = _mt[_mti++];
        y ^= TEMPERING_SHIFT_U(y);
        y ^= TEMPERING_SHIFT_S(y) & _temperingMaskB;
        y ^= TEMPERING_SHIFT_T(y) & _temperingMaskC;
        y ^= TEMPERING_SHIFT_L(y);

        //reals: (0,1)-interval
        //return y; for integer generation
        return ((double)y * _finalConstant);
    }
    
    /// <summary>
    /// Generate a random number between 0 and 1
    /// </summary>
    /// <returns></returns>
    public double Generate()
    {
        return Genrand();
    }

    /// <summary>
    /// Generate an int between two bounds
    /// </summary>
    /// <param name="lowerBound">The lower bound (inclusive)</param>
    /// <param name="higherBound">The higher bound (inclusive)</param>
    /// <returns></returns>
    public int Next(int lowerBound, int higherBound)
    {
        if (higherBound < lowerBound)
        {
            throw new ArgumentException("Higher bound must be greater than lower bound");
        }

        return Convert.ToInt32(Math.Floor(Next(lowerBound * 1.0d, higherBound * 1.0d)));
    }

    /// <summary>
    /// Generates a random integer within the range [0, bound).
    /// </summary>
    /// <param name="bound">The exclusive upper bound for the random number. Must be greater than 0.</param>
    /// <returns>A random integer greater than or equal to 0 and less than <paramref name="bound"/>.</returns>
    public int Next(int bound)
    {
        return Next(0, bound);
    }

    public float NextSingle() => (float)Generate();
    
    /// <summary>
    /// Generate a double between two bounds
    /// </summary>
    /// <param name="lowerBound">The lower bound (inclusive)</param>
    /// <param name="higherBound">The higher bound (exclusive)</param>
    /// <returns>The random num.</returns>
    public double Next(double lowerBound, double higherBound)
    {
        if (higherBound < lowerBound)
        {
            throw new ArgumentException("Higher bound must be greater than lower bound");
        }
        return Generate() * (higherBound - lowerBound) + lowerBound;
    }
    
    public float Next(float lowerBound, float higherBound)
    {
        if (higherBound < lowerBound)
        {
            throw new ArgumentException("Higher bound must be greater than lower bound");
        }
        return (float)(Generate() * (higherBound - lowerBound) + lowerBound);
    }
}