namespace System
{
    public static class TypeExtensions
    {
        public static string GetFullTypeAndAssemblyName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.FullName + ", " + type.Assembly.FullName;
        }
    }
}
