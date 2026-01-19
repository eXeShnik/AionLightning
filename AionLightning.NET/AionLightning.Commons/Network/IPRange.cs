using System;
using System.Net;

namespace AionLightning.Commons.Network
{
    public class IPRange
    {
        private readonly IPAddress _min;
        private readonly IPAddress _max;
        private readonly IPAddress _address;

        public IPRange(string min, string max, string address)
        {
            _min = IPAddress.Parse(min);
            _max = IPAddress.Parse(max);
            _address = IPAddress.Parse(address);
        }

        public bool IsInRange(string address)
        {
            var addr = IPAddress.Parse(address);
            return IsInRange(addr);
        }

        public bool IsInRange(IPAddress address)
        {
            var addrBytes = address.GetAddressBytes();
            var minBytes = _min.GetAddressBytes();
            var maxBytes = _max.GetAddressBytes();

            bool lower = true;
            for (int i = 0; i < addrBytes.Length; i++)
            {
                if (addrBytes[i] < minBytes[i])
                {
                    lower = false;
                    break;
                }
            }

            bool upper = true;
            for (int i = 0; i < addrBytes.Length; i++)
            {
                if (addrBytes[i] > maxBytes[i])
                {
                    upper = false;
                    break;
                }
            }

            return lower && upper;
        }

        public IPAddress GetAddress()
        {
            return _address;
        }

        public IPAddress GetMinAsIPAddress()
        {
            return _min;
        }

        public IPAddress GetMaxAsIPAddress()
        {
            return _max;
        }

        public override bool Equals(object obj)
        {
            if (obj is IPRange other)
            {
                return _min.Equals(other._min) && _max.Equals(other._max) && _address.Equals(other._address);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_min, _max, _address);
        }
    }
}
