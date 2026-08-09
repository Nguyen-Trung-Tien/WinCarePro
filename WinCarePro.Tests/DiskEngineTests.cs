using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCarePro.Engines;
using Xunit;

namespace WinCarePro.Tests;

public class DiskEngineTests
{
    [Fact]
    public void GetDiskHealthStatus_ReturnsDriveList()
    {
        var engine = new DiskEngine();
        var drives = engine.GetDiskHealthStatus();
        
        Assert.NotNull(drives);
        Assert.NotEmpty(drives);
        Assert.All(drives, d => Assert.False(string.IsNullOrEmpty(d.Name)));
    }

    [Fact]
    public async Task AnalyzeStorageAsync_CalculatesSizesAndPercentages()
    {
        var engine = new DiskEngine();
        string tempDir = Path.Combine(Path.GetTempPath(), "WinCare_Test_Storage_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            string file1 = Path.Combine(tempDir, "file1.txt");
            string file2 = Path.Combine(tempDir, "file2.txt");
            File.WriteAllBytes(file1, new byte[1000]);
            File.WriteAllBytes(file2, new byte[3000]);

            var items = await engine.AnalyzeStorageAsync(tempDir);

            Assert.NotNull(items);
            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Name == "file2.txt" && Math.Abs(i.Percentage - 75.0) < 1.0);
            Assert.Contains(items, i => i.Name == "file1.txt" && Math.Abs(i.Percentage - 25.0) < 1.0);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task FindDuplicateFilesAsync_IdentifiesExactDuplicates_And_RejectsSameHeaderDifferentBody()
    {
        var engine = new DiskEngine();
        string tempDir = Path.Combine(Path.GetTempPath(), "WinCare_Test_Dup_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Create exact duplicates (2MB each)
            byte[] dummyBody = new byte[70 * 1024]; // > 64KB
            new Random(42).NextBytes(dummyBody);

            string dup1 = Path.Combine(tempDir, "dup1.bin");
            string dup2 = Path.Combine(tempDir, "dup2.bin");
            File.WriteAllBytes(dup1, dummyBody);
            File.WriteAllBytes(dup2, dummyBody);

            // 2. Create false-positive candidate: Same size, SAME first 64KB, but DIFFERENT tail
            byte[] header = dummyBody.Take(64 * 1024).ToArray();
            byte[] falseCandidate = new byte[70 * 1024];
            Array.Copy(header, 0, falseCandidate, 0, header.Length);
            // Put different random bytes in the tail
            new Random(99).NextBytes(falseCandidate.AsSpan(64 * 1024));

            string falseDup = Path.Combine(tempDir, "false_dup.bin");
            File.WriteAllBytes(falseDup, falseCandidate);

            var duplicates = await engine.FindDuplicateFilesAsync(tempDir);

            Assert.NotNull(duplicates);
            Assert.Single(duplicates); // Exactly 1 group
            var group = duplicates[0];
            Assert.Equal(2, group.FilePaths.Count);
            Assert.Contains(dup1, group.FilePaths);
            Assert.Contains(dup2, group.FilePaths);
            Assert.DoesNotContain(falseDup, group.FilePaths); // Crucial check: false candidate must NOT be included!
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ClearEmptyFoldersAsync_DeletesRecursiveEmptyFolders()
    {
        var engine = new DiskEngine();
        string tempDir = Path.Combine(Path.GetTempPath(), "WinCare_Test_Empty_" + Guid.NewGuid());
        string nestedEmpty = Path.Combine(tempDir, "Level1", "Level2", "Level3");
        Directory.CreateDirectory(nestedEmpty);

        try
        {
            int count = await engine.ClearEmptyFoldersAsync(tempDir);

            Assert.True(count >= 3);
            Assert.False(Directory.Exists(nestedEmpty));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
