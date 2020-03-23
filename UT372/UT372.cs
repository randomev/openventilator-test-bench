using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uni_T_Devices
{

    static class StringExtensions
    {

        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

    }

    public class UT372
    {

        Int16[] bytesToDigits = {
           0x7B,
           0x60,
           0x5E,
           0x7C,
           0x65,
           0x3D,
           0x3F,
           0x70,
           0x7F,
           0x7D,
        };

        const int DECIMAL_POINT_MASK = 0x80;

        public double parseSerialInputToRPM(string serialInput)
        {
            // https://sigrok.org/gitweb/?p=libsigrok.git;a=blob;f=src/dmm/ut372.c

            double rpm = 0;
            // RAW DATA:
            // 070?<3=7<60655>607;007885

            // first character is not read
            // 0 70 ?< 3= 7< 60   65 5> 60 7; 00     78 85
            // X 1  2  3   4  5   6  7  8  9  10     11 12
            //   --- R  P  M --   --- TIME  ----     OTHER LCD ELEMENTS

            string rpmPart = serialInput.Substring(1, 10);
            var parts = rpmPart.SplitInParts(2);
            int i = 0;
            foreach (string item in parts)
            {

                char a = (char)item[0];
                char b = (char)item[1];

                if (a > 0x39)
                    a = (char)((int)a + 7);
                if (b > 0x39)
                    b = (char)((int)b + 7);

                char[] chars = { a, b };

                string c = new string(chars);
                int intValue = int.Parse(c, System.Globalization.NumberStyles.HexNumber);

                for (int j=0;j<bytesToDigits.Length;j++)
                { 
                    if (bytesToDigits[j] == (intValue & ~DECIMAL_POINT_MASK))
                    {
                        rpm = rpm + j * Math.Pow(10, i);
                    }
                }
                i = i + 1;
            }

            return rpm;
        }

        
  //      /* Decode a pair of characters into a byte. */
  //57 static uint8_t decode_pair(const uint8_t* buf)
  //58 {
  //59         unsigned int i;
  //60         char hex[3];
  //61 
  //62         hex[2] = '\0';
  //63 
  //64         for (i = 0; i< 2; i++) {
  //65                 hex[i] = buf[i];
  //66                 if (hex[i] > 0x39)
  //67                         hex[i] += 7;
  //68         }
  //69 
  //70         return strtol(hex, NULL, 16);
  //71 }

    }
}
