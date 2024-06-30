# <img src="https://raw.githubusercontent.com/marticliment/WingetUI/main/media/icon.png" height="40">UniGetUI

[![Downloads@latest](https://img.shields.io/github/downloads/marticliment/WingetUI/3.0.2/total?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe)
[![Release Version Badge](https://img.shields.io/github/v/release/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases)
[![Issues Badge](https://img.shields.io/github/issues/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/issues)
[![Closed Issues Badge](https://img.shields.io/github/issues-closed/marticliment/WingetUI?color=%238256d0&style=for-the-badge)](https://github.com/marticliment/WingetUI/issues?q=is%3Aissue+is%3Aclosed)<br>
The main goal of this project is to create an intuitive GUI for the most common CLI package managers for Windows 10 and 11, such as [Winget](https://learn.microsoft.com/en-us/windows/package-manager/), [Scoop](https://scoop.sh/), [Chocolatey](https://chocolatey.org/), [Pip](https://pypi.org/), [Npm](https://www.npmjs.com/), [.NET Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) and [PowerShell Gallery](https://www.powershellgallery.com/).
With this app, you can easily download, install, update, and uninstall any software published on the supported package managers — and much more!

Check out the [Supported Package Managers Table](#supported-package-managers) for more details!

**This is UniGetUI (formerly WingetUI)'s official repository. If you are searching for UniGetUI's homepage, please refer to [https://www.marticliment.com/unigetui/](https://www.marticliment.com/unigetui/)**

**Disclaimer:** This project has no connection with any supported package managers — it's completely unofficial. Be aware that I, the developer of UniGetUI, am NOT responsible for the downloaded software. Proceed with caution

![Endpoint Badge](https://img.shields.io/endpoint?url=https%3A%2F%2Fmarticliment.com%2Fresources%2Fbadges%2Fdev-status.json)
 
## Table of contents
 - **[UniGetUI Homepage](https://www.marticliment.com/unigetui/)**
 - [Table of contents](#table-of-contents)
 - [Installation](#installation)
 - [Update UniGetUI](#update-UniGetUI)
 - [Support the developer](#support-the-developer)
 - [Features](#features)
   - [Supported Package Managers](#supported-package-managers)
 - [Translating UniGetUI](#translating-UniGetUI-to-other-languages)
   - [Currently supported languages](#currently-supported-languages)
 - [Contributors](#contributors)
 - [Screenshots](#screenshots)
 - [Frequently Asked Questions](#frequently-asked-questions)

 
## Installation
<p>There are multiple ways to install UniGetUI — choose whichever one you prefer!<br</p>

**Download UniGetUI installer (recommended):**
<p align="left"><b><a href="https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe">Click here to download UniGetUI</a></b></p>

**Install UniGetUI through Winget:**    
```cmd
winget install --exact wingetui --source winget
```

**Install UniGetUI through Scoop:**
```cmd
scoop bucket add extras
```
```cmd
scoop install extras/wingetui
```

**Install UniGetUI through Chocolatey:**    
```cmd
choco install wingetui
```


## Update UniGetUI

UniGetUI has a built-in autoupdater. However, it can also be updated like any other package within UniGetUI (since UniGetUI is available through Winget and Scoop).


## Support the developer

It really does make a big difference, and is very much appreciated. Thanks :)

<a href='https://ko-fi.com/martinet101' target='_blank'><img style='border:0px;height:36px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>


## Features

 - Install, update, and remove software from your system easily at one click: UniGetUI combines the packages from the most used package managers for windows: Winget, Chocolatey, Scoop, Pip, Npm and .NET Tool.
 - Discover new packages and filter them to easily find the package you want.
 - View detailed metadata about any package before installing it. Get the direct download URL or the name of the publisher, as well as the size of the download.
 - Easily bulk-install, update, or uninstall multiple packages at once selecting multiple packages before performing an operation
 - Automatically update packages, or be notified when updates become available. Skip versions or completely ignore updates on a per-package basis.
 - Manage your available updates at the touch of a button from the **Widgets pane** or from **Dev Home** pane with [Widgets for UniGetUI](https://apps.microsoft.com/detail/9NB9M5KZ8SLX)*.
 - The system tray icon will also show the available updates and installed packages, to efficiently update a program or remove a package from your system.
 - Easily customize how and where packages are installed. Select different installation options and switches for each package. Install an older version or force to install a 32 bit architecture. \[But don't worry, those options will be saved for future updates for this package*]
 - Share packages with your friends to show them off that program you found. Here is an example: [Hey \@friend, Check out this program!](https://marticliment.com/unigetui/share/?pname=Google%20Chrome&pid=Google.Chrome&psource=Winget:%20winget)
 - Export custom lists of packages to then import them to another machine and install those packages with previously specified, custom installation parameters. Setting up machines or configuring a specific software setup has never been easier.
 - Backup your packages to a local file to easily recover your setup in a matter of seconds when migrating to a new machine*

## Supported Package Managers

**NOTE:** All package managers do support basic install, update, and uninstall processes, as well as checking for updates, finding new packages, and retrieving details from a package.

| Manager | Skip integrity checks | Interactive installation | Install Older Versions | Install a PreRelease Version | Install a Custom Architecture | Install on a Custom Scope | Custom Install Location | Custom Package Sources | Supported since |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Winget** | ✅ | ⚠️³ | ✅ | ☑️² | ✅ | ⚠️¹ | ⚠️¹ | ✅ | 0.1.0 |
| **Scoop** | ✅ | ❌ | ❌ | ☑️² | ✅ | ✅ | ❌ | ✅ | 0.1.0 |
| **Chocolatey** | ✅ | ⚠️³ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | 1.6.0 |
| **Npm** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | 2.0.0 |
| **Pip** | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | 2.0.0 |
| **.NET Tool** | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | 2.1.0 |
| **PowerShell** | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | ✅ | 2.2.0 |

✅: Supported on UniGetUI<br>
☑️: Not directly supported but can be easily achieved<br>
⚠️: Some packages might not follow this setting<br>
❌: Not supported by the Package Manager<br>
<br>
**1\.** Some packages do not support installing to a custom location or scope and will ignore this setting<br>
**2\.** Despite the Package Manager may not support _PreReleases_, some packages can be found duplicated, with one of the copies being the beta version of it.<br>
**3\.** Some installers do not have a GUI, and will ignore the `interactive` flag<br>

# Translating UniGetUI to other languages
To translate UniGetUI to other languages or to update an old translation, please see [Translating UniGetUI - UniGetUI Wiki](https://github.com/marticliment/WingetUI/wiki#translating-wingetui) for more info.


## Currently Supported languages
<!-- Autogenerated translations -->
| Language | Translated | Translator(s) |
| :-- | :-- | --- |
| <img src='https://flagcdn.com/sa.svg' width=20> &nbsp; Arabic - عربي‎ | 80% | [Abdu11ahAS](https://github.com/Abdu11ahAS), [Abdullah-Dev115](https://github.com/Abdullah-Dev115), [FancyCookin](https://github.com/FancyCookin), [mo9a7i](https://github.com/mo9a7i) |
| <img src='https://flagcdn.com/bg.svg' width=20> &nbsp; Bulgarian - български | 63% | Vasil Kolev |
| <img src='https://flagcdn.com/bd.svg' width=20> &nbsp; Bangla - বাংলা | 87% | [fluentmoheshwar](https://github.com/fluentmoheshwar), [itz-rj-here](https://github.com/itz-rj-here), Mushfiq Iqbal Rayon, Nilavra Bhattacharya |
| <img src='https://flagcdn.com/ad.svg' width=20> &nbsp; Catalan - Català | 100% | [marticliment](https://github.com/marticliment) |
| <img src='https://flagcdn.com/cz.svg' width=20> &nbsp; Czech - Čeština | 100% | [mlisko](https://github.com/mlisko), [panther7](https://github.com/panther7) |
| <img src='https://flagcdn.com/dk.svg' width=20> &nbsp; Danish - Dansk | 78% | [AAUCrisp](https://github.com/AAUCrisp), [mikkolukas](https://github.com/mikkolukas), [yrjarv](https://github.com/yrjarv) |
| <img src='https://flagcdn.com/de.svg' width=20> &nbsp; German - Deutsch | 99% | [1270o1](https://github.com/1270o1), [alxhu-dev](https://github.com/alxhu-dev), [CanePlayz](https://github.com/CanePlayz), [Datacra5H](https://github.com/Datacra5H), [ebnater](https://github.com/ebnater), [michaelmairegger](https://github.com/michaelmairegger), [Seeloewen](https://github.com/Seeloewen), [yrjarv](https://github.com/yrjarv) |
| <img src='https://flagcdn.com/gr.svg' width=20> &nbsp; Greek - Ελληνικά | 99% | [antwnhsx](https://github.com/antwnhsx), [thunderstrike116](https://github.com/thunderstrike116), [wobblerrrgg](https://github.com/wobblerrrgg) |
| <img src='https://flagcdn.com/gb.svg' width=20> &nbsp; English - English | 100% | [marticliment](https://github.com/marticliment), [ppvnf](https://github.com/ppvnf) |
| <img src='https://flagcdn.com/es.svg' width=20> &nbsp; Spanish - Castellano | 100% | [apazga](https://github.com/apazga), [dalbitresb12](https://github.com/dalbitresb12), [evaneliasyoung](https://github.com/evaneliasyoung), [guplem](https://github.com/guplem), [JMoreno97](https://github.com/JMoreno97), [marticliment](https://github.com/marticliment), [rubnium](https://github.com/rubnium), [uKER](https://github.com/uKER) |
| <img src='https://flagcdn.com/ir.svg' width=20> &nbsp; Persian - فارسی‎ | 49% | [itsarian](https://github.com/itsarian), [Mahdi-Hazrati](https://github.com/Mahdi-Hazrati), [smsi2001](https://github.com/smsi2001) |
| <img src='https://flagcdn.com/fi.svg' width=20> &nbsp; Finnish - Suomi | 1% |  |
| <img src='https://flagcdn.com/fr.svg' width=20> &nbsp; French - Français | 99% | BreatFR, Evans Costa, Rémi Guerrero, [W1L7dev](https://github.com/W1L7dev) |
| <img src='https://flagcdn.com/in.svg' width=20> &nbsp; Hindi - हिंदी | 56% | [atharva_xoxo](https://github.com/atharva_xoxo), [satanarious](https://github.com/satanarious) |
| <img src='https://flagcdn.com/hr.svg' width=20> &nbsp; Croatian - Hrvatski | 60% | Stjepan Treger |
| <img src='https://flagcdn.com/il.svg' width=20> &nbsp; Hebrew - עִבְרִית‎ | 50% | Oryan |
| <img src='https://flagcdn.com/hu.svg' width=20> &nbsp; Hungarian - Magyar | 100% | [gidano](https://github.com/gidano) |
| <img src='https://flagcdn.com/it.svg' width=20> &nbsp; Italian - Italiano | 100% | David Senoner, [giacobot](https://github.com/giacobot), [maicol07](https://github.com/maicol07), [mapi68](https://github.com/mapi68), [mrfranza](https://github.com/mrfranza), Rosario Di Mauro, [supertost100](https://github.com/supertost100) |
| <img src='https://flagcdn.com/id.svg' width=20> &nbsp; Indonesian - Bahasa Indonesia | 88% | [arthackrc](https://github.com/arthackrc), [joenior](https://github.com/joenior) |
| <img src='https://flagcdn.com/jp.svg' width=20> &nbsp; Japanese - 日本語 | 83% | [nob-swik](https://github.com/nob-swik), sho9029, [tacostea](https://github.com/tacostea), Yuki Takase |
| <img src='https://flagcdn.com/kr.svg' width=20> &nbsp; Korean - 한국어 | 82% | [minbert](https://github.com/minbert), [shblue21](https://github.com/shblue21) |
| <img src='https://flagcdn.com/mk.svg' width=20> &nbsp; Macedonian - Македонски | 66% | LordDeatHunter |
| <img src='https://flagcdn.com/no.svg' width=20> &nbsp; Norwegian (bokmål) | 99% | [yrjarv](https://github.com/yrjarv) |
| <img src='https://flagcdn.com/no.svg' width=20> &nbsp; Norwegian (nynorsk) | 99% | [yrjarv](https://github.com/yrjarv) |
| <img src='https://flagcdn.com/nl.svg' width=20> &nbsp; Dutch - Nederlands | 100% | [abbydiode](https://github.com/abbydiode), [Stephan-P](https://github.com/Stephan-P) |
| <img src='https://flagcdn.com/pl.svg' width=20> &nbsp; Polish - Polski | 99% | [KamilZielinski](https://github.com/KamilZielinski), [kwiateusz](https://github.com/kwiateusz), [RegularGvy13](https://github.com/RegularGvy13) |
| <img src='https://flagcdn.com/br.svg' width=20> &nbsp; Portuguese (Brazil) | 99% | [maisondasilva](https://github.com/maisondasilva), [ppvnf](https://github.com/ppvnf), [Rodrigo-Matsuura](https://github.com/Rodrigo-Matsuura), [wanderleihuttel](https://github.com/wanderleihuttel) |
| <img src='https://flagcdn.com/pt.svg' width=20> &nbsp; Portuguese (Portugal) | 100% | [PoetaGA](https://github.com/PoetaGA), [Tiago_Ferreira](https://github.com/Tiago_Ferreira) |
| <img src='https://flagcdn.com/ro.svg' width=20> &nbsp; Romanian - Română | 99% | [SilverGreen93](https://github.com/SilverGreen93), TZACANEL |
| <img src='https://flagcdn.com/ru.svg' width=20> &nbsp; Russian - Русский | 82% | [bropines](https://github.com/bropines), [DvladikD](https://github.com/DvladikD), [flatron4eg](https://github.com/flatron4eg), [katrovsky](https://github.com/katrovsky), Sergey, sklart |
| <img src='https://flagcdn.com/rs.svg' width=20> &nbsp; Serbian - Srpski | 70% | [daVinci13](https://github.com/daVinci13), Nemanja Djurcic |
| <img src='https://flagcdn.com/lk.svg' width=20> &nbsp; Sinhala - සිංහල | 6% | [SashikaSandeepa](https://github.com/SashikaSandeepa) |
| <img src='https://flagcdn.com/si.svg' width=20> &nbsp; Slovene - Slovenščina | 100% | [rumplin](https://github.com/rumplin) |
| <img src='https://flagcdn.com/ph.svg' width=20> &nbsp; Tagalog - Tagalog | 15% | lasersPew |
| <img src='https://flagcdn.com/th.svg' width=20> &nbsp; Thai - ภาษาไทย | 70% | [apaeisara](https://github.com/apaeisara), [dulapahv](https://github.com/dulapahv) |
| <img src='https://flagcdn.com/tr.svg' width=20> &nbsp; Turkish - Türkçe | 99% | [ahmetozmtn](https://github.com/ahmetozmtn), [dogancanyr](https://github.com/dogancanyr), [gokberkgs](https://github.com/gokberkgs) |
| <img src='https://flagcdn.com/ua.svg' width=20> &nbsp; Ukranian - Yкраї́нська | 59% | Artem Moldovanenko, Operator404 |
| <img src='https://flagcdn.com/vn.svg' width=20> &nbsp; Vietnamese - Tiếng Việt | 99% | [legendsjoon](https://github.com/legendsjoon), [txavlog](https://github.com/txavlog) |
| <img src='https://flagcdn.com/cn.svg' width=20> &nbsp; Simplified Chinese (China) | 100% | Aaron Liu, adfnekc, [arthurfsy2](https://github.com/arthurfsy2), [bai0012](https://github.com/bai0012), BUGP Association, ciaran, CnYeSheng, Cololi, [FloatStream](https://github.com/FloatStream), [SpaceTimee](https://github.com/SpaceTimee), Yisme |
| <img src='https://flagcdn.com/tw.svg' width=20> &nbsp; Traditional Chinese (Taiwan) | 72% | Aaron Liu, CnYeSheng, Cololi, [yrctw](https://github.com/yrctw) |

Last updated: Sun Mar 31 00:12:11 2024
<!-- END Autogenerated translations -->


# Contributions
 UniGetUI wouldn't have been possible without the help of our dear contributors. From the person who fixed a typo to the person who improved half of the code, UniGetUI wouldn't be possible without them! :smile:<br><br>

## Contributors:
 [![My dear contributors](https://contrib.rocks/image?repo=marticliment/WingetUI)](https://github.com/marticliment/WingetUI/graphs/contributors)<br><br>
 

# Screenshots
 
![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_1.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_2.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_3.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_4.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_5.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_6.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_7.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_8.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_9.png)


# Frequently asked questions

**Q: I am unable to install or upgrade a specific Winget package! What should I do?**<br>

A: This is likely an issue with Winget rather than UniGetUI. 

Please check if it's possible to install/upgrade the package through PowerShell or the Command Prompt by using the commands `winget upgrade` or `winget install`, depending on the situation (for example: `winget upgrade --id Microsoft.PowerToys`). 

If this doesn't work, consider asking for help at [Winget's project page](https://github.com/microsoft/winget-cli).<br>

#

**Q: The name of a package is trimmed with ellipsis — how do I see its full name/id?**<br>

A: This is a known limitation of Winget. 

See more details on issue https://github.com/microsoft/winget-cli/issues/2603.<br>

#

**Q: My antivirus is telling me that UniGetUI is a virus! / My browser is blocking the download of UniGetUI!**<br>

A: A common reason apps (i.e., executables) get blocked and/or detected as a virus — even when there's nothing malicious about them, like in the case of UniGetUI — is because they're not being used by a relatively large amount of people.

Combine that with the fact that you might be downloading something recently released, and simply blocking unknown apps is in many cases a good precaution to take to prevent actual malware.

Since UniGetUI is open source and safe to use, simply whitelist the app in the settings of your antivirus/browser.<br>

#

**Q: Are Winget/Scoop packages safe?**<br>

A: UniGetUI, Microsoft and Scoop aren't responsible for the packages available for download, which are provided by third parties and can theoretically be compromised.

To mitigate the risks of downloading malware, Microsoft has implemented a few checks for the software available on Winget. Even so, It's recommended to only download software from publishers that you trust. 

<br><p align="center"><i>Check out the <a href="https://github.com/marticliment/WingetUI/wiki">Wiki</a> for more information!</i></p>

## Command-line parameters:
`--daemon`: Start UniGetUI without spawning a new window. UniGetUI will run minimized on the system tray. UniGetUI is called with this parameter when launched at startup. **Autostart UniGetUI in the notifications area** must be enabled for this parameter to work.<br>
`--welcomewizard` (or simply `--welcome`): Show a window to choose which package managers to use.<br>
`--updateapps`: Enable automatic installation of available updates.
`--report-all-errors`: May help debug crashes

### Installer command-line parameters:
The installer is inno-setup based, so it supports regular Inno Setup command-line parameters. Additionally, it also supports the following parameters:
 <br>`/NoAutoStart`: Will not launch UniGetUI after installing it.
<br> `/ALLUSERS`: Install UniGetUI for every user
<br> `/CURRENTUSER`: Install UniGetUI for the current user only
