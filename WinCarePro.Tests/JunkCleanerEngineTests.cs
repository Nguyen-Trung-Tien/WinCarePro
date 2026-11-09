using System;
using System.IO;
using System.Reflection;
using Xunit;
using WinCarePro.Engines;

namespace WinCarePro.Tests;

public class JunkCleanerEngineTests
{
    [Fact]
    public void IsFileLocked_ReadOnlyFile_ReturnsFalse()
    {
        // Arrange
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test data");
            File.SetAttributes(tempFile, FileAttributes.ReadOnly);

            // Act
            var method = typeof(JunkCleanerEngine).GetMethod("IsFileLocked", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            
            var isLocked = method.Invoke(null, new object[] { tempFile });
            Assert.NotNull(isLocked);

            // Assert
            Assert.False((bool)isLocked);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.SetAttributes(tempFile, FileAttributes.Normal);
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void JunkType_ContainsDeveloperCache()
    {
        Assert.True(Enum.IsDefined(typeof(WinCarePro.Models.JunkType), WinCarePro.Models.JunkType.DeveloperCache));
    }

    [Fact]
    public void JunkCategory_FormatProperties_ReturnAccurateStrings()
    {
        var category = new WinCarePro.Models.JunkCategory
        {
            Name = "Developer & IDE Caches",
            Type = WinCarePro.Models.JunkType.DeveloperCache,
            SizeBytes = 10485760, // 10 MB
            CleanableBytes = 10485760,
            LockedBytes = 0,
            FileCount = 42
        };

        Assert.Equal("42 files", category.FileCountFormatted);
        Assert.Contains("10", category.SizeFormatted);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, category.LockedSizeVisibility);
    }

    [Fact]
    public void JunkSafetyTier_CorrectlyCategorizes_StandardTypes()
    {
        var tempCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.WindowsTemp };
        var browserCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.BrowserCache };
        var devCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.DeveloperCache };

        Assert.Equal(WinCarePro.Models.JunkSafetyTier.Safe, tempCat.SafetyTier);
        Assert.Equal(WinCarePro.Models.JunkSafetyTier.Review, browserCat.SafetyTier);
        Assert.Equal(WinCarePro.Models.JunkSafetyTier.Developer, devCat.SafetyTier);
    }

    [Fact]
    public void JunkSafetyTier_HelperProperties_ReturnCorrectVisualColorsAndGlyphs()
    {
        var safeCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.UserTemp };
        Assert.Equal("#FF10B981", safeCat.SafetyTierColor);
        Assert.Equal("\uE73E", safeCat.SafetyTierGlyph);

        var reviewCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.BrowserCache };
        Assert.Equal("#FFF59E0B", reviewCat.SafetyTierColor);
        Assert.Equal("\uE7BA", reviewCat.SafetyTierGlyph);

        var devCat = new WinCarePro.Models.JunkCategory { Type = WinCarePro.Models.JunkType.DeveloperCache };
        Assert.Equal("#FFA78BFA", devCat.SafetyTierColor);
        Assert.Equal("\uE943", devCat.SafetyTierGlyph);
    }

    [Fact]
    public async Task JunkViewModel_ExportReportAsync_GeneratesValidCsv()
    {
        var vm = new WinCarePro.ViewModels.JunkViewModel();
        var cat = new WinCarePro.Models.JunkCategory
        {
            Name = "Windows Temp Files",
            Type = WinCarePro.Models.JunkType.WindowsTemp,
            CleanableBytes = 2048,
            LockedBytes = 1024,
            FileCount = 15,
            FolderPath = @"C:\Windows\Temp"
        };
        vm.Categories.Add(cat);

        string? path = await vm.ExportReportAsync();

        Assert.NotNull(path);
        Assert.True(File.Exists(path));

        try
        {
            string content = await File.ReadAllTextAsync(path);
            Assert.Contains("Category,SafetyTier", content);
            Assert.Contains("Windows Temp Files", content);
            Assert.Contains(@"C:\Windows\Temp", content);
        }
        finally
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }

    [Fact]
    public void JunkViewModel_SearchAndSort_FiltersAccurately()
    {
        var vm = new WinCarePro.ViewModels.JunkViewModel();
        var cat1 = new WinCarePro.Models.JunkCategory
        {
            Name = "Alpha Logs",
            Description = "System event diagnostic logs",
            CleanableBytes = 5000,
            Type = WinCarePro.Models.JunkType.SystemLog
        };
        var cat2 = new WinCarePro.Models.JunkCategory
        {
            Name = "Beta Browser Cache",
            Description = "Web browser cached images and cookies",
            CleanableBytes = 100000,
            Type = WinCarePro.Models.JunkType.BrowserCache
        };

        vm.Categories.Add(cat1);
        vm.Categories.Add(cat2);

        // Apply filter
        vm.SearchQuery = "Browser";
        // vm.ApplyFilters() is called on property set

        Assert.Single(vm.Categories);
        Assert.Equal("Beta Browser Cache", vm.Categories[0].Name);

        // Clear query
        vm.SearchQuery = "";
        Assert.Equal(2, vm.Categories.Count);
        // Default sort is SizeDesc: Beta (100000) should be first
        Assert.Equal("Beta Browser Cache", vm.Categories[0].Name);
        Assert.Equal("Alpha Logs", vm.Categories[1].Name);
    }
}
