using System;
using System.Text;

namespace VectorTileRenderer
{
    static class Utils
    {
        public static double ConvertRange(double oldValue, double oldMin, double oldMax, double newMin, double newMax, bool clamp = false)
        {
            double NewRange;
            double NewValue;
            double OldRange = (oldMax - oldMin);
            if (OldRange == 0)
            {
                NewValue = newMin;
            }
            else
            {
                NewRange = (newMax - newMin);
                NewValue = (((oldValue - oldMin) * NewRange) / OldRange) + newMin;
            }

            if (clamp)
            {
                NewValue = Math.Min(Math.Max(NewValue, newMin), newMax);
            }

            return NewValue;
        }

        public static string Sha256(string randomString)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
