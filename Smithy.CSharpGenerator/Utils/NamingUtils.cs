using System.Text;

namespace Smithy.CSharpGenerator.Utils
{
    public static class NamingUtils
    {
        public static string PascalCase(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var parts = id.Split(new[] { '.', '#', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1));
            }
            return sb.ToString();
        }
    }
}
