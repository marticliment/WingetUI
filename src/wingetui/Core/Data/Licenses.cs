using System;
using System.Collections.Generic;

namespace UniGetUI.Core.Data
{
    public static class LicenseData
    {
        public static Dictionary<string, string> LicenseNames = new() {
            {"WingetUI",                "LGPL v2.1" },

            // C# Libraries
            {"Pickers",                 "MIT"},
            {"Community Toolkit",       "MIT"},
            {"H.NotifyIcon",            "MIT"},
            {"Windows App Sdk",         "MIT"},
            {"NancyFx",                 "MIT"},
            {"YamlDotNet",              "MIT"},
            {"InnoDependencyInstaller", "CPOL 1.02" }, 
            
            // Package managers and related
            {"Winget",                  "MIT"},
            {"Scoop",                   "MIT"},
            {"Scoop Search",            "MIT"},
            {"Chocolatey",              "Apache v2"},
            {"Npm",                     "Artistic License 2.0"},
            {"Pip",                     "MIT"},
            {"Parse Pip Search",        "MIT"},
            {"PowerShell Gallery",      "Unknown"},
            {"Dotnet Tool",             "Free (Proprietary license)"},
            {"dotnet-tools-outdated",   "MIT"},

            // Other
            {"Gsudo",                   "MIT"},
            {"Icons",                   "By Icons8"},
        };

        public static Dictionary<string, Uri> LicenseURLs = new(){
            {"WingetUI",                new Uri("https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html")},
                                       
            // C# Libraries            
            {"Pickers",                 new Uri("https://github.com/PavlikBender/Pickers/blob/master/LICENSE")},
            {"Community Toolkit",       new Uri("https://github.com/CommunityToolkit/Windows/blob/main/License.md")},
            {"H.NotifyIcon",            new Uri("https://github.com/HavenDV/H.NotifyIcon/blob/master/LICENSE.md")},
            {"Windows App Sdk",         new Uri("https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE")},
            {"NancyFx",                 new Uri("https://github.com/NancyFx/Nancy/blob/master/license.txt")},
            {"YamlDotNet",              new Uri("https://github.com/aaubry/YamlDotNet/blob/master/LICENSE.txt") },
            {"InnoDependencyInstaller", new Uri("https://github.com/DomGries/InnoDependencyInstaller/blob/master/LICENSE.md") },
                
            // Package managers and related
            {"Winget",                  new Uri("https://github.com/microsoft/winget-cli/blob/master/LICENSE")},
            {"Scoop",                   new Uri("https://github.com/ScoopInstaller/Scoop/blob/master/LICENSE")},
            {"Scoop Search",            new Uri("https://github.com/shilangyu/scoop-search/blob/master/LICENSE")},
            {"Chocolatey",              new Uri("https://github.com/chocolatey/choco/blob/develop/LICENSE")},
            {"Npm",                     new Uri("https://github.com/npm/cli/blob/latest/LICENSE")},
            {"Pip",                     new Uri("https://github.com/pypa/pip/blob/main/LICENSE.txt")},
            {"Parse Pip Search",        new Uri("https://github.com/marticliment/parseable_pip_search/blob/master/LICENSE.md")},
            {"Dotnet Tool",             new Uri("https://dotnet.microsoft.com/en-us/platform/free")},
            {"dotnet-tools-outdated",   new Uri("https://github.com/rychlym/dotnet-tools-outdated/blob/master/LICENSE")},
            {"PowerShell Gallery",      new Uri("https://www.powershellgallery.com/")},
                                       
            // Other                   
            {"Gsudo",                   new Uri("https://github.com/gerardog/gsudo/blob/master/LICENSE.txt")},
            {"Icons",                   new Uri("https://icons8.com/license")},
        };

        public static Dictionary<string, Uri> HomepageUrls = new(){
            {"WingetUI",                new Uri("https://marticliment.com/wingetui")},
            
            // C# Libraries
            {"Pickers",                 new Uri("https://github.com/PavlikBender/Pickers/")},
            {"Community Toolkit",       new Uri("https://github.com/CommunityToolkit/Windows/")},
            {"H.NotifyIcon",            new Uri("https://github.com/HavenDV/H.NotifyIcon/")},
            {"Windows App Sdk",         new Uri("https://github.com/microsoft/WindowsAppSDK/")},
            {"NancyFx",                 new Uri("https://github.com/NancyFx/Nancy/")},
            {"YamlDotNet",              new Uri("https://github.com/aaubry/YamlDotNet/") },
            {"InnoDependencyInstaller", new Uri("https://github.com/DomGries/InnoDependencyInstaller")},
            
            // Package managers and related
            {"Winget",                  new Uri("https://github.com/microsoft/winget-cli/")},
            {"Scoop",                   new Uri("https://github.com/ScoopInstaller/Scoop/")},
            {"Scoop Search",            new Uri("https://github.com/shilangyu/scoop-search/")},
            {"Chocolatey",              new Uri("https://github.com/chocolatey/choco/")},
            {"Npm",                     new Uri("https://github.com/npm/cli/")},
            {"Pip",                     new Uri("https://github.com/pypa/pip/")},
            {"Parse Pip Search",        new Uri("https://github.com/marticliment/parseable_pip_search/")},
            {"Dotnet Tool",             new Uri("https://github.com/dotnet/sdk/")},
            {"dotnet-tools-outdated",   new Uri("https://github.com/rychlym/dotnet-tools-outdated/")},
            {"PowerShell Gallery",      new Uri("https://www.powershellgallery.com/")},
                                       
            // Other                   
            {"Gsudo",                   new Uri("https://github.com/gerardog/gsudo/")},
            {"Icons",                   new Uri("https://icons8.com")},
        };

    }
}
