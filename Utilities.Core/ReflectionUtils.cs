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
  }
}
