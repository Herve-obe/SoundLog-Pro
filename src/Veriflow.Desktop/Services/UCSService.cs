using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Services
{
    public interface IUCSService
    {
        List<UCSCategory> GetAllCategories();
        List<string> GetMainCategories();
        List<UCSCategory> GetSubCategories(string mainCategory);
        UCSCategory? FindByCatID(string catId);
        UCSCategory? FindByNames(string category, string subCategory);
    }

    public class UCSService : IUCSService
    {
        private readonly List<UCSCategory> _allCategories = new();

        public UCSService()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            try
            {
                // Try to load from Data folder in output directory
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UCSCategories.json");
                
                if (!File.Exists(jsonPath))
                {
                    // Fallback: try relative path
                    jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "UCSCategories.json");
                }

                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var db = JsonSerializer.Deserialize<UCSCategoryDatabase>(json, options);
                    
                    if (db != null)
                    {
                        // Flatten the database into a list of UCSCategory objects
                        foreach (var categoryGroup in db.Categories)
                        {
                            foreach (var subCat in categoryGroup.SubCategories)
                            {
                                _allCategories.Add(new UCSCategory
                                {
                                    Category = categoryGroup.Name,
                                    SubCategory = subCat.Name,
                                    CatID = subCat.CatID
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - UCS is optional
                System.Diagnostics.Debug.WriteLine($"Failed to load UCS database: {ex.Message}");
            }
        }

        public List<UCSCategory> GetAllCategories()
        {
            return _allCategories.ToList();
        }

        public List<string> GetMainCategories()
        {
            return _allCategories
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public List<UCSCategory> GetSubCategories(string mainCategory)
        {
            return _allCategories
                .Where(c => c.Category.Equals(mainCategory, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.SubCategory)
                .ToList();
        }

        public UCSCategory? FindByCatID(string catId)
        {
            return _allCategories
                .FirstOrDefault(c => c.CatID.Equals(catId, StringComparison.OrdinalIgnoreCase));
        }

        public UCSCategory? FindByNames(string category, string subCategory)
        {
            return _allCategories
                .FirstOrDefault(c => 
                    c.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                    c.SubCategory.Equals(subCategory, StringComparison.OrdinalIgnoreCase));
        }
    }
}
