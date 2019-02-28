# Introduction 
Factory Test Framework

TODO: Explain different projects

# Usage
To use FTF UWP on FactoryOS:
1) Build appx package for target hardware. DO NOT enable .NET Native in the project settings.
2) Deploy appx and dependencies via Windows Device Portal.
3) If communicating with FTF Service on the same device, enable loopback for UWP (https://docs.microsoft.com/en-us/previous-versions/windows/apps/hh780593(v%3dwin.10)), the PFN is factorytestframeworkuwp_5f3hxk13kzdkt:

    checknetisolation loopbackexempt -a -n=FactoryTestFrameworkUWP_5f3hxk13kzdkt
    
4) Register app to start on first boot by setting HKLM\Software\Microsoft\CoreShell\FactoryOS\DefaultApp to REG_SZ with value "factorytestframeworkuwp_5f3hxk13kzdkt!App" (This only works if device is in State Separation development mode or this is done offline.)

To use FTF Service on FactoryOS:
1) Publish FTFService project using Visual Studio
2) Copy files to FactoryOS device
3) Run "FTFService.exe action:install name:FTFService" (The created service entry only persists if device is in State Separation development mode.)
4) Run "sc start FTFService"
5) If communicating with FTF clients on other devices, create the required firewall rules:

    netsh advfirewall firewall add rule name=ftfservice_tcp_in program=<Path to FTFService.exe> protocol=tcp dir=in enable=yes action=allow profile=public,private,domain

    netsh advfirewall firewall add rule name=ftfservice_tcp_out program=<Path to FTFService.exe> protocol=tcp dir=out enable=yes action=allow profile=public,private,domain

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://www.visualstudio.com/en-us/docs/git/create-a-readme). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)