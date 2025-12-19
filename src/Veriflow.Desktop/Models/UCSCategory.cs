namespace Veriflow.Desktop.Models
{
    /// <summary>
    /// Represents a UCS (Universal Category System) category for sound effects.
    /// UCS is a public domain standard for categorizing audio files.
    /// </summary>
    public class UCSCategory
    {
        /// <summary>
        /// Main category name (e.g., "AMBIENCES", "ANIMALS", "FOLEY")
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// Subcategory name (e.g., "URBAN", "NATURE", "FOOTSTEPS")
        /// </summary>
        public string SubCategory { get; set; } = "";

        /// <summary>
        /// Category ID - condensed form of Category + SubCategory (e.g., "AMBUrban", "ANMBird")
        /// </summary>
        public string CatID { get; set; } = "";

        /// <summary>
        /// Full hierarchical path for display (e.g., "AMBIENCES > URBAN")
        /// </summary>
        public string FullPath => string.IsNullOrEmpty(SubCategory) 
            ? Category 
            : $"{Category} > {SubCategory}";

        /// <summary>
        /// Display name for UI (CatID if available, otherwise FullPath)
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(CatID) 
            ? $"{CatID} ({FullPath})" 
            : FullPath;
    }

    /// <summary>
    /// Container for UCS category database
    /// </summary>
    public class UCSCategoryDatabase
    {
        public List<UCSCategoryGroup> Categories { get; set; } = new();
    }

    /// <summary>
    /// Group of subcategories under a main category
    /// </summary>
    public class UCSCategoryGroup
    {
        public string Name { get; set; } = "";
        public List<UCSSubCategory> SubCategories { get; set; } = new();
    }

    /// <summary>
    /// Subcategory with its CatID
    /// </summary>
    public class UCSSubCategory
    {
        public string Name { get; set; } = "";
        public string CatID { get; set; } = "";
    }
}
