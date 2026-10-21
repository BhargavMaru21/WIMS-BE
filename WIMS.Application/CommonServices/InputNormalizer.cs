using System.Reflection;
using WIMS.Application.Interfaces.Common;

namespace WIMS.Application.Common.Services;

public class InputNormalizer : IInputNormalizer
{
    public string Normalize(string value)
    {
        return value.Trim();
    }

    public string NormalizeEmail(string value)
    {
    return value.Trim().ToLower();
    }

    public T NormalizeObject<T>(T obj)
    {
        if (obj == null)
        {
            return obj!;
        }

        PropertyInfo[] properties =
            typeof(T).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType != typeof(string) ||
                !property.CanWrite)
            {
                continue;
            }

            string? currentValue = property.GetValue(obj) as string;

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                continue;
            }

            currentValue = currentValue.Trim();

            if (property.Name.Contains("Email",StringComparison.OrdinalIgnoreCase))
            {
                currentValue = currentValue.ToLower();
            }

            property.SetValue(obj, currentValue);
        }

        return obj;
    }
}