using System;
using System.Reflection;

namespace ClintonMead
{
    public static class ReflectionUtils
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = obj.GetType().GetField(name, bindingFlags);
            if (field != null) {
                return (T) field.GetValue(obj);
            }
            else
            {
                throw new Exception("Unable to find field: " + name);
            }
        }

        public static void InvokeMethod(this object obj, string methodName, params object[] parameters)
        {
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo dynMethod = obj.GetType().GetMethod(methodName, bindingFlags);
            dynMethod.Invoke(obj, parameters);
        }
    }
}
