
namespace dropkick.Configuration.Dsl.Iis
{
    public interface IisSiteInstallOptions : IisSiteOptions
    {
        IisVirtualDirectoryInstallOptions VirtualDirectory(string name);
    }
}
