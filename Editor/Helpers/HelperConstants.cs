namespace NOVA.Scripts
{
    public static class HelperConstants
    {
        public const int CameraWidth = 640;
        public const int CameraHeight = 480;
        public const float MinWindowHeight = 1280f;
        public const float MinWindowLength = 720f;

        public const string ResourcesDirectory = "Resources";
        public const string GestureAssetsDirName = "GestureAssets";

        public const string GestureListNoFilters = "No Filter";
        public const string NoSorting = "No Sorting";
        public const string SortAlphabetically = "A-Z";
        public const string SortInReverse = "Z-A";

        public static readonly string[] SortingOptions = {
            NoSorting,
            SortAlphabetically,
            SortInReverse
        };
    }
}
