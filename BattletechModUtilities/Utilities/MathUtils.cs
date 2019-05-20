using System;

namespace BattletechModUtilities
{
    public static class MathUtils
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.LessThan(min) ? min : val.GreaterThan(max) ? max : val;
        }

        public static bool LessThan<T>(this T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) < 0;
        }

        public static bool GreaterThan<T>(this T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) > 0;
        }

        public static bool LessThanOrEqual<T>(this T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool GreaterThanOrEqual<T>(this T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) >= 0;
        }
    }
}
