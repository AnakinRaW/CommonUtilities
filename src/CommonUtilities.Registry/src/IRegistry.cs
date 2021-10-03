namespace Sklavenwalker.CommonUtilities.Registry
{
    public interface IRegistry
    {
        IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view);
    }
}
