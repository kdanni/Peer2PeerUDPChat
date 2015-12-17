using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Peer2PeerChat.Controler
{
    public partial class MeshLogic
    {

        public static string generateMAC_Hash(string salt)
        {

            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();


            string concatenatedMACs = (salt == null) ? "" : salt;

            foreach (NetworkInterface adapter in nics)
            {
                PhysicalAddress address = adapter.GetPhysicalAddress();
                concatenatedMACs += address.ToString();
            }

            Debug.WriteLine(concatenatedMACs);

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.Unicode.GetBytes(concatenatedMACs);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            Debug.WriteLine(sb.ToString());

            return sb.ToString();

        }
    }
}
