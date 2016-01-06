using System;

namespace IOT.Device
{
    public static class NetworkHostConverter
    {
        public static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }

        public static short HostToNetworkOrder(short host)
        {
            return (short)(((int)host & (int)byte.MaxValue) << 8 | (int)host >> 8 & (int)byte.MaxValue);
        }

        public static long HostToNetworkOrder(long host)
        {
            return ((long)HostToNetworkOrder((int)host) & (long)uint.MaxValue) << 32 | (long)HostToNetworkOrder((int)(host >> 32)) & (long)uint.MaxValue;
        }

        public static int HostToNetworkOrder(int host)
        {
            return ((int)HostToNetworkOrder((short)host) & (int)ushort.MaxValue) << 16 | (int)HostToNetworkOrder((short)(host >> 16)) & (int)ushort.MaxValue;
        }
    }
}
