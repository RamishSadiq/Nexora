namespace Nexora.Architecture.Tests;

public sealed class ArchitectureConventionTests
{
    [Fact]
    public void Product_assemblies_use_the_nexora_prefix()
    {
        var productAssemblies = Directory
            .EnumerateFiles(FindRepositoryRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}backend{Path.DirectorySeparatorChar}"))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        Assert.NotEmpty(productAssemblies);
        Assert.All(productAssemblies, name => Assert.StartsWith("Nexora.", name));
    }

    [Fact]
    public void Modules_depend_only_on_published_building_block_contracts()
    {
        foreach(var path in Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(),"backend","src","Modules"),"*.csproj",SearchOption.AllDirectories))
        {
            var project=System.Xml.Linq.XDocument.Load(path);
            var references=project.Descendants("ProjectReference").Select(x=>x.Attribute("Include")!.Value).ToArray();
            Assert.NotEmpty(references);Assert.All(references,reference=>Assert.Contains("BuildingBlocks",reference));
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
