using System;
using System.Collections.Concurrent;

namespace BattletechModUtilities
{
    public class CachedFunction<TIn, TOut>
    {
        private readonly ConcurrentDictionary<TIn, Lazy<TOut>> _internalDict 
            = new ConcurrentDictionary<TIn, Lazy<TOut>>();

        private readonly Func<TIn, TOut> _f;

        private CachedFunction(Func<TIn, TOut> f)
        {
            _f = f;
        }

        public TOut GetValue(TIn key) {
            return _internalDict.GetOrAdd(key, new Lazy<TOut>(() => _f(key))).Value;
        }

        public static Func<TIn, TOut> CacheFunction(Func<TIn, TOut> f)
        {
            CachedFunction<TIn, TOut> cachedFunction = new CachedFunction<TIn, TOut>(f);
            return cachedFunction.GetValue;
        }

    }

    public static class CachedFunction
    {
        public static Func<TIn, TOut> CacheFunction<TIn, TOut>(this Func<TIn, TOut> f)
        {
            return CachedFunction<TIn, TOut>.CacheFunction(f);
        }

        public static Func<TIn1, TIn2, TOut> CacheFunction<TIn1, TIn2, TOut>(this Func<TIn1, TIn2, TOut> f)
        {
            Func<ValueTuple<TIn1, TIn2>, TOut> fTupled = CacheFunction<ValueTuple<TIn1, TIn2>, TOut>(x => f(x.Item1, x.Item2));

            return (x1, x2) => fTupled(ValueTuple.Create(x1, x2));
        }
    }
}
