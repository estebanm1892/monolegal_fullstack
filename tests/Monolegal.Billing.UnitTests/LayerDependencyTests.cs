using System.Xml.Linq;

namespace Monolegal.Billing.UnitTests;

public class LayerDependencyTests
{
    [Theory]
    [InlineData("Domain", new string[0])]
    [InlineData("Application", new[] { "Monolegal.Billing.Domain.csproj" })]
    public void InnerLayersOnlyReferenceAllowedProjects(string layer, string[] allowedProjects)
    {
        var project = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Architecture", $"{layer}.csproj"));
        var references = project.Descendants("ProjectReference")
            .Select(reference => Path.GetFileName(reference.Attribute("Include")!.Value.Replace('\\', '/')))
            .Order().ToArray();

        Assert.Equal(allowedProjects.Order(), references);
        Assert.Empty(project.Descendants("FrameworkReference"));
        Assert.DoesNotContain(project.Descendants("PackageReference"), reference =>
        {
            var name = reference.Attribute("Include")!.Value;
            return name.StartsWith("MongoDB", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("MailKit", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("MimeKit", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase);
        });
    }
}
