using Sitecore.Data.Items;

namespace SharedSource.RedirectModule.Helpers
{
    /// <summary>
    /// Item Extension class
    /// </summary>
    public static class ItemExtensions
    {
        public static int GetIntegerFieldValue(this Item item, string fieldName, int defaultValue)
        {
            if (item == null)
            {
                return defaultValue;
            }

            var field = item.Fields[fieldName];

            if (field != null && field.HasValue)
            {
                if (int.TryParse(field.Value, out var result))
                {
                    return result;
                }
            }

            return defaultValue;
        }
    }
}
