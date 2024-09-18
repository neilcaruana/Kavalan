using System.Reflection;

namespace Kavalan.Core;

public static class ResourceHelper
{
    public static string ReadResourceFile(string resourceFullName)
    {
        var assembly = Assembly.GetCallingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(resourceFullName);
        if (stream == null)
            throw new FileNotFoundException($"Resource {resourceFullName} not found.");

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
