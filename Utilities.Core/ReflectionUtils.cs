using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Utilities.Core
{
  public class ReflectionUtils
  {
    public static List<String> GetPublicPropertyNames<T>()
    {
      return
        typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(pi => pi.Name)
        .ToList();
    }

    public static String GetPublicPropertyValues<T>(Object source)
    {
      var type = source.GetType();
      return
        GetPublicPropertyNames<T>()
        .Select(propertyName => propertyName + " = " + (type.GetProperty(propertyName).GetValue(source, null) ?? "NULL"))
        .ToList()
        .Join(Environment.NewLine);
    }

    public static String GetPropertyValues(Object source, BindingFlags bindingFlags)
    {
      var type = source.GetType();
      return
        type
        .GetProperties(bindingFlags)
        .Select(propertyInfo => propertyInfo.Name + " = " + (type.GetProperty(propertyInfo.Name).GetValue(source, null) ?? "NULL"))
        .ToList()
        .Join(Environment.NewLine);
    }

    /// <summary>
    /// Given an object instance, return a string containing the object literal construction expression used to
    /// build the instance.  Useful in T4 templates.
    /// <para>
    /// The returned string contains only those property settings that differ from the instance's default property values.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that has a parameterless constructor.</typeparam>
    /// <param name="instance"></param>
    /// <param name="propertiesToIgnore"></param>
    /// <returns></returns>
    public static String GetObjectInitializer<T>(T instance, params String[] propertiesToIgnore) where T : class, new()
    {
      var result = new List<String>();

      var t = typeof(T);
      var defaultInstance = new T();
      var publicInstanceProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var propertyInfo in publicInstanceProperties)
      {
        if (propertyInfo.GetIndexParameters().Any())
          continue;

        if (propertiesToIgnore.ContainsCI(propertyInfo.Name))
          continue;

        var defaultInstanceValue = propertyInfo.GetValue(defaultInstance);
        var newInstanceValue = propertyInfo.GetValue(instance);
        if (!Object.Equals(defaultInstanceValue, newInstanceValue))
          result.Add(String.Format("{0} = {1}", propertyInfo.Name, GetLiteralDisplayValue(newInstanceValue)));
      }

      return String.Format("new {0}() {{ {1} }}", t.Name, result.OrderBy(s => s).Join(", "));
    }

    private static String GetLiteralDisplayValue(Object value)
    {
      if (value is Char)
      {
        return String.Concat("'", value, "'");
      }
      else if (value is String)
      {
        return String.Concat("\"", value, "\"");
      }
      else if (value is Enum)
      {
        var ve = (value as Enum);
        var typename = ve.GetType().Name;
        return
          ve
          .ToString()
          .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
          .Select(v => String.Concat(typename, ".", v.Trim()))
          .OrderBy(s => s)
          .Join(" | ");
      }
      else
      {
        return value.ToString();
      }
    }
  }
}
