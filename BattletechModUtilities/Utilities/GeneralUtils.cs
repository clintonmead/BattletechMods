using System;

namespace BattletechModUtilities
{
    public static class GeneralUtils
    {
        public static TOut MapNull<TIn, TOut>(this TIn x, Func<TIn, TOut> f) where TOut : class
        {
            return ReferenceEquals(x, null) ? null : f(x);
        }
    }
}
