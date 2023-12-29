using System.Text;

namespace UserManager.Tools
{
    public static class IEnumerableToStringHelper
    {
        public static string ConvertToString(this IEnumerable<IConfigurationSection> enumerable)
        {
            var builder = new StringBuilder();
            foreach (var item in enumerable)
            {
                builder.Append(item.Value);
                builder.Append(",");
            }
            return builder.ToString().TrimEnd(',');
        }
    }

}
