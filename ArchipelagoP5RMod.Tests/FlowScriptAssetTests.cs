using System.IO;
using Xunit;

namespace ArchipelagoP5RMod.Tests;

public class FlowScriptAssetTests
{
    private static string ProjectRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ArchipelagoP5RMod"));

    [Fact]
    public void FlowScriptDstFiles_MustNotContainUtf8Bom()
    {
        string srcDir = Path.Combine(ProjectRoot, "FlowFiles", "src");
        Assert.True(Directory.Exists(srcDir), $"Directory not found: {srcDir}");

        string[] dstFiles = Directory.GetFiles(srcDir, "*.dst");
        Assert.NotEmpty(dstFiles);

        foreach (string dstFile in dstFiles)
        {
            byte[] bytes = File.ReadAllBytes(dstFile);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.False(hasBom, $"File {Path.GetFileName(dstFile)} contains UTF-8 BOM which breaks cmd batch parsing!");
        }
    }

    [Fact]
    public void FlowScriptSourceFiles_ExistAndAreNonEmpty()
    {
        string srcDir = Path.Combine(ProjectRoot, "FlowFiles", "src");
        string[] flowFiles = Directory.GetFiles(srcDir, "*.flow");
        Assert.NotEmpty(flowFiles);

        foreach (string flowFile in flowFiles)
        {
            FileInfo fi = new FileInfo(flowFile);
            Assert.True(fi.Length > 0, $"Flow script source file {fi.Name} is 0 bytes!");
        }
    }

    [Fact]
    public void CompiledBfFiles_ExistInP5REssentialsDirectory()
    {
        string initDir = Path.Combine(ProjectRoot, "P5REssentials", "CPK", "en.cpk", "field", "init");
        string scriptFieldDir = Path.Combine(ProjectRoot, "P5REssentials", "CPK", "en.cpk", "script", "field");

        Assert.True(Directory.Exists(initDir), $"P5REssentials init dir missing: {initDir}");
        Assert.True(Directory.Exists(scriptFieldDir), $"P5REssentials script field dir missing: {scriptFieldDir}");

        string[] initBfs = Directory.GetFiles(initDir, "*.bf");
        string[] fieldBfs = Directory.GetFiles(scriptFieldDir, "*.bf");

        Assert.NotEmpty(initBfs);
        Assert.NotEmpty(fieldBfs);
    }
}
