using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FTFService
{

    public interface IComputingService
    {
        float AddFloat(float x, float y);
    }

}
