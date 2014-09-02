
using System;
using System.IO;
using System.Reflection;

namespace Gemcom.Klabr
{
    public static class ApplicationExtensions
    {
        public static string GetLocalUserAppDataPath()
        {
            string path = string.Empty;
            Assembly assembly = Assembly.GetEntryAssembly();

            if (assembly != null)
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine(
                        Path.Combine(
                            GetCompanyName(assembly),
                            assembly.GetName().Name),
                        GetFormattedVersion(assembly)
                        )
                    );
            }

            return path;
        }

        private static string GetCompanyName(Assembly assembly)
        {
            string name = string.Empty;
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);

            if (attributes != null && attributes.Length > 0)
            {
                AssemblyCompanyAttribute assemblyCompanyAttribute = attributes[0] as AssemblyCompanyAttribute;

                if (assemblyCompanyAttribute != null)
                    name = assemblyCompanyAttribute.Company;
            }

            return name;
        }

        private static string GetFormattedVersion(Assembly assembly)
        {
            Version assemblyVersion = assembly.GetName().Version;

            string version = string.Format("{0}.{1}.{2}.{3}",
                                           assemblyVersion.Major,
                                           assemblyVersion.Minor,
                                           assemblyVersion.Build,
                                           assemblyVersion.Revision);

            return version;
        }
    }
}
