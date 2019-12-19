BotCore:5 Plugin [OpenCollarBot]
=====


This plugin is designed to be used only with BotCore:5

This code is here mainly to show how it all works. If you want to submit a pull request feel free.

Prerequisites: 

```
cinderblocks/LibreMetaverse: 392e139
zontreck/BotCore5: 5.1.2.0 (NOT PUBLIC YET)

NuGet:
Newtonsoft.Json: 12.0.2
Octokit.Net: 0.36.0
```

```xml
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="12.0.2" />
    <PackageReference Include="Octokit" Version="0.36.0" />
  </ItemGroup>
```


No other dependencies are necessary. Once you can compile against both libraries, you can produce the DLL and place it in the Bot.exe directory.
