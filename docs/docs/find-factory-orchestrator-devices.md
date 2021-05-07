
# Find devices running Factory Orchestrator on your local network
The Factory Orchestrator service supports [DNS Service Discovery](http://www.dns-sd.org/) (DNS-SD), which allows you to easily to query your local network for devices running Factory Orchestrator with [network access enabled](service-configuration.md#network-access)! (If network access is not enabled, the service does not advertise to DNS-SD.)

Factory Orchestrator service instances advertise under `<HostName>_factorch._tcp.local`, with the port the service uses (by default 45684) contained in the SRV record.

## FindDevice
FindDevice is an open source, cross-platform, .NET command line tool that you can use to look for devices running Factory Orchestrator service via DNS-SD. You can view the source code and/or download it at [https://github.com/microsoft/FindDevice/](https://github.com/microsoft/FindDevice/).

![image of FindDevice](https://user-images.githubusercontent.com/31931010/117501891-8c7b0f00-af33-11eb-94d7-6b4ee4b6e090.png)

### Find devices with an API
[net-mdns](https://github.com/richardschneider/net-mdns) is a good choice if you want to discover Factory Orchestrator devices in your .NET code. The [FindDevice source code](https://github.com/microsoft/FindDevice/) may be a good template to follow.