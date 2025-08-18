namespace NOVA.Scripts
{
    public enum GestureImageExtension
    {
        Jpeg,
        Jpg,
        Png
    }

    public static class GestureImageExtensionMethods
    {
        public static string GetExtension(this GestureImageExtension extension)
        {
            return extension switch
            {
                GestureImageExtension.Jpeg => "jpeg",
                GestureImageExtension.Jpg => "jpg",
                GestureImageExtension.Png => "png",
                _ => throw new System.ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
        }
    }
}
