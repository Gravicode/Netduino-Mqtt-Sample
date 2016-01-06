using System;
using Microsoft.SPOT;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace IOT.Device
{

    /// <summary>
    /// The ICMP protocol header used with the IPv4 protocol.
    /// </summary>
    class IcmpHeader
    {
        private ushort icmpChecksum;               // Checksum of ICMP header and payload
        private ushort icmpId;                     // Message ID
        private ushort icmpSequence;               // ICMP sequence number

        static public byte EchoRequestType = 8;     // ICMP echo request
        static public byte EchoRequestCode = 0;     // ICMP echo request code
        static public byte EchoReplyType = 0;     // ICMP echo reply
        static public byte EchoReplyCode = 0;     // ICMP echo reply code

        static public int IcmpHeaderLength = 8;    // Length of ICMP header

        /// <summary>
        /// Default constructor for ICMP packet
        /// </summary>
        public IcmpHeader()
        {
            Type = 0;
            Code = 0;
            icmpChecksum = 0;
            icmpId = 0;
            icmpSequence = 0;
        }

        /// <summary>
        /// ICMP message type.
        /// </summary>
        public byte Type { get; set; }

        /// <summary>
        /// ICMP message code.
        /// </summary>
        public byte Code { get; set; }

        /// <summary>
        /// Checksum of ICMP packet and payload.  Performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get
            {
                return (ushort)NetworkHostConverter.NetworkToHostOrder((short)icmpChecksum);
            }
            set
            {
                icmpChecksum = (ushort)NetworkHostConverter.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// ICMP message ID. Used to uniquely identify the source of the ICMP packet.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Id
        {
            get
            {
                return (ushort)NetworkHostConverter.NetworkToHostOrder((short)icmpId);
            }
            set
            {
                icmpId = (ushort)NetworkHostConverter.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// ICMP sequence number. As each ICMP message is sent the sequence should be incremented.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Sequence
        {
            get
            {
                return (ushort)NetworkHostConverter.NetworkToHostOrder((short)icmpSequence);
            }
            set
            {
                icmpSequence = (ushort)NetworkHostConverter.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// This routine builds the ICMP packet suitable for sending on a raw socket.
        /// It builds the ICMP packet and payload into a byte array and computes
        /// the checksum.
        /// </summary>
        /// <param name="payLoad">Data payload of the ICMP packet</param>
        /// <returns>Byte array representing the ICMP packet and payload</returns>
        public byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            byte[] icmpPacket,
                    byteValue;
            int offset = 0;

            icmpPacket = new byte[IcmpHeaderLength + payLoad.Length];

            icmpPacket[offset++] = Type;
            icmpPacket[offset++] = Code;
            icmpPacket[offset++] = 0;          // Zero out the checksum until the packet is assembled
            icmpPacket[offset++] = 0;

            byteValue = BitConverter.GetBytes(icmpId);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(icmpSequence);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            if (payLoad.Length > 0)
            {
                Array.Copy(payLoad, 0, icmpPacket, offset, payLoad.Length);
                offset += payLoad.Length;
            }

            // Compute the checksum over the entire packet
            Checksum = ComputeChecksum(icmpPacket);

            // Put the checksum back into the packet
            byteValue = BitConverter.GetBytes(icmpChecksum);
            Array.Copy(byteValue, 0, icmpPacket, 2, byteValue.Length);

            return icmpPacket;
        }

        public byte[] BuildPacket(ushort pingID, byte[] payLoad)
        {
            IcmpHeader protocolHeader = new IcmpHeader() { Id = pingID, Sequence = 0, Type = IcmpHeader.EchoRequestType, Code = IcmpHeader.EchoRequestCode }; ;

            payLoad = protocolHeader.GetProtocolPacketBytes(payLoad);
            
            return payLoad;
        }

        static public ushort ComputeChecksum(byte[] payLoad)
        {
            uint xsum = 0;
            ushort shortval = 0,
                    hiword = 0,
                    loword = 0;

            // Sum up the 16-bits
            for (int i = 0; i < payLoad.Length / 2; i++)
            {
                hiword = (ushort)(((ushort)payLoad[i * 2]) << 8);
                loword = (ushort)payLoad[(i * 2) + 1];

                shortval = (ushort)(hiword | loword);

                xsum = xsum + (uint)shortval;
            }
            // Pad if necessary
            if ((payLoad.Length % 2) != 0)
            {
                xsum += (uint)payLoad[payLoad.Length - 1];
            }

            xsum = ((xsum >> 16) + (xsum & 0xFFFF));
            xsum = (xsum + (xsum >> 16));
            shortval = (ushort)(~xsum);

            return shortval;
        }
    }

    
}
