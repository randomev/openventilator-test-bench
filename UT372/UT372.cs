using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using System.Collections.ObjectModel;

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
        public static UsbDevice MyUsbDevice;


        /*
            * Length:18
               DescriptorType:Device
               BcdUsb:0x0100
               Class:PerInterface
               SubClass:0x00
               Protocol:0x00
               MaxPacketSize0:8
               VendorID:0x1A86
               ProductID:0xE008
               BcdDevice:0x1400
               ManufacturerStringIndex:1
               ProductStringIndex:2
               SerialStringIndex:0
               ConfigurationCount:1
               ManufacturerString:WCH.CN 
               ProductString:USB to Serial
               SerialString:
            */
        public static DateTime LastDataEventDate = DateTime.Now;

        #region SET YOUR USB Vendor and Product ID!

        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x1A86, 0xE008);

        #endregion

        public void openUSB()
        {
                ErrorCode ec = ErrorCode.None;

                try
                {
                // Find and open the usb device.
                //MyUsbDevice = UsbDevice.AllDevices.Find(device => device.Pid == 0xE008 && device.Vid == 0x1A86);

                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                    
                // If the device is open and ready
                if (MyUsbDevice == null) throw new Exception("Device Not Found.");

                    // If this is a "whole" usb device (libusb-win32, linux libusb)
                    // it will have an IUsbDevice interface. If not (WinUSB) the 
                    // variable will be null indicating this is an interface of a 
                    // device.
                    
                    IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // This is a "whole" USB device. Before it can be used, 
                        // the desired configuration and interface must be selected.

                        // Select config #1
                        wholeUsbDevice.SetConfiguration(1);

                        // Claim interface #0.
                        wholeUsbDevice.ClaimInterface(0);
                    }

                    // open read endpoint 1.
                    UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep02,8,EndpointType.Interrupt);

                // open write endpoint 1.
                UsbEndpointWriter writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02,EndpointType.Interrupt);

                // Remove the exepath/startup filename text from the begining of the CommandLine.
                //string cmdLine = 0x0960; ///Regex.Replace(Environment.CommandLine, "^\".+?\"^.*? |^.*? ", "", RegexOptions.Singleline);
                byte[] bytesToSend = { 0x09, 0x60, 0, 0, 3 }; // config for 2400 baud

                //if (!String.IsNullOrEmpty(cmdLine))
                //{
                //ErrorCode ec2 = ErrorCode.None;

                int bytesWritten;
                //ec = writer.Write(bytesToSend, 2000, out bytesWritten);
                //if (ec != ErrorCode.None) throw new Exception(UsbDevice.LastErrorString);


                reader.ReadBufferSize = 8;
                reader.DataReceived += (OnRxEndPointData);
                reader.DataReceivedEnabled = true;

                LastDataEventDate = DateTime.Now;
                while ((DateTime.Now - LastDataEventDate).TotalMilliseconds < 1000)
                {
                    Console.Write(".");

                }

                // Always disable and unhook event when done.
                reader.DataReceivedEnabled = false;
                reader.DataReceived -= (OnRxEndPointData);

                byte[] readBuffer = new byte[8];
                //while (ec == ErrorCode.None)
                //{
                //    int bytesRead;

                //    // If the device hasn't sent data in the last 100 milliseconds,
                //    // a timeout error (ec = IoTimedOut) will occur. 
                //    //ec = reader.ReadFlush();
                //    //for (int i = 0; i < 10; i++)
                //    //{
                //    //    ec = reader.Read(readBuffer, 100, out bytesRead);

                //    //    //if (bytesRead == 0) throw new Exception("No more bytes!");

                //    //    // Write that output to the console.
                //    //    Console.Write(Encoding.Default.GetString(readBuffer, 0, bytesRead));
                //    //}
                //}

                Console.WriteLine("\r\nDone!\r\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
                }
                finally
                {
                    if (MyUsbDevice != null)
                    {
                        if (MyUsbDevice.IsOpen)
                        {
                            // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                            // it exposes an IUsbDevice interface. If not (WinUSB) the 
                            // 'wholeUsbDevice' variable will be null indicating this is 
                            // an interface of a device; it does not require or support 
                            // configuration and interface selection.
                            IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                            if (!ReferenceEquals(wholeUsbDevice, null))
                            {
                                // Release interface #0.
                                wholeUsbDevice.ReleaseInterface(0);
                            }

                            MyUsbDevice.Close();
                        }
                        MyUsbDevice = null;

                        // Free usb resources
                        UsbDevice.Exit();

                    }

                    // Wait for user input..
                    //Console.ReadKey();
                }
        }

        private static void OnRxEndPointData(object sender, EndpointDataEventArgs e)
        {
           //LastDataEventDate = DateTime.Now;
            Console.Write(Encoding.Default.GetString(e.Buffer, 0, e.Count));
        }
        public void dumpUSBData()
        {
            // Dump all devices and descriptor information to console output.
            UsbRegDeviceList allDevices = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in allDevices)
            {
                if (usbRegistry.Open(out MyUsbDevice))
                {
                    Console.WriteLine(MyUsbDevice.Info.ToString());
                    for (int iConfig = 0; iConfig < MyUsbDevice.Configs.Count; iConfig++)
                    {
                        UsbConfigInfo configInfo = MyUsbDevice.Configs[iConfig];
                        Console.WriteLine(configInfo.ToString());

                        ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.InterfaceInfoList;
                        for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
                        {
                            UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
                            Console.WriteLine(interfaceInfo.ToString());

                            ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.EndpointInfoList;
                            for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
                            {
                                Console.WriteLine(endpointList[iEndpoint].ToString());
                            }
                        }
                    }
                }
            }


            // Free usb resources.
            // This is necessary for libusb-1.0 and Linux compatibility.
            UsbDevice.Exit();

        }

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
