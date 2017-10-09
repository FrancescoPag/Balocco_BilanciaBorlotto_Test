using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlotto_Test
{
    public class BorlottoReaderCom
    {
        /*
         * Acknowledge string to respond is: cfm\n
         * Send "cfb 0\r\n" to permanently turn the E240 off; or "cfb 128\r\n" to turn it back on again.
         * This command is only valid for BT 2.1 devices but not for the BT 4.0.
         * https://stackoverflow.com/questions/36066334/how-to-read-data-from-leica-disto-via-bluetooth
         * */

        const int BUFFER_SIZE = 512;
        const int BAUD_RATE = 9600;
        const Parity PARITY_BIT = Parity.None;
        const int DATA_BITS = 8;
        const StopBits STOP_BITS = StopBits.One;

        private SerialPort _port;
        private string _bufferString;
        private char[] _bufferChars;
        private Logger logger = LogManager.GetCurrentClassLogger();

        public BorlottoReaderCom(string port)
        {
            _port = new SerialPort(port);
            logger.Log(LogLevel.Trace, $"SerialPortCOM: {port}");
            _bufferString = String.Empty;
            _bufferChars = new char[BUFFER_SIZE];
        }

        public bool Open()
        {
            try
            {
                if (!_port.IsOpen)
                {
                    _port.Open();
                    logger.Log(LogLevel.Trace, $"Port open");
                }
            }
            catch(Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error while opening the port.");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
            return _port.IsOpen;
        }

        public bool Close()
        {
            try
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                    logger.Log(LogLevel.Trace, $"Port closed");
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error while closing the port.");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
            return !_port.IsOpen;
        }

        public string Read()
        {
            logger.Log(LogLevel.Trace, $"Reading");
            string ret = null;
            try
            {
                if (!_port.IsOpen)
                    _port.Open();

                _bufferString = String.Empty;
                Array.Clear(_bufferChars, 0, BUFFER_SIZE);
                int readCount = 0;
                char c;

                do
                {
                    c = (char)_port.ReadChar();
                    logger.Log(LogLevel.Trace, $"ReadChar {c} [count:{readCount+1}]");

                    if (readCount < BUFFER_SIZE)
                        _bufferChars[readCount++] = c;
                    _bufferString += c;

                } while (c != '\r');
                ret = _bufferString;
                logger.Log(LogLevel.Info, $"Read ended {ret} [count:{readCount}]");
            }
            catch(Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error while reading.");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
            return ret; 
        }
    }

    public class BorlottoReaderBluetooth
    {
        const int BUFFER_SIZE = 512;
        private string _bufferString;
        private char[] _bufferChars;
        private Logger logger = LogManager.GetCurrentClassLogger();
        
        private List<BluetoothDeviceInfo> deviceList = new List<BluetoothDeviceInfo>();
        private BluetoothAddress _endpointAddress;
        BluetoothEndPoint localEndpoint;
        BluetoothClient localClient;
        NetworkStream stream;

        public BorlottoReaderBluetooth()
        {
            _bufferChars = new char[BUFFER_SIZE];
            _endpointAddress = GetLocalEndpointAddress();
            if (_endpointAddress == null)
            {
                logger.Log(LogLevel.Info, $"No local bluetooth endpoint found.");
                return;
            }
            logger.Log(LogLevel.Info, $"Local Bluetooth Endpoint Address: {_endpointAddress.ToString()}");

            try
            {
                localEndpoint = new BluetoothEndPoint(_endpointAddress, BluetoothService.SerialPort);
                localClient = new BluetoothClient(localEndpoint);
                logger.Trace("Discovering devices...");
                Console.WriteLine("Discovering devices...");
                deviceList = new List<BluetoothDeviceInfo>(localClient.DiscoverDevices());
                logger.Trace("Device discovering completed.");

                if (deviceList.Count == 0)
                {
                    Console.WriteLine("No devices discovered.");
                    logger.Trace("No devices discovered.");
                    return;
                }

                Console.WriteLine("Available devices:");
                for (int i = 0; i < deviceList.Count; ++i)
                {
                    Console.WriteLine($"{i + 1}.\t {deviceList[i].DeviceName} {deviceList[i].DeviceAddress} [Connected:{deviceList[i].Connected}] [Authenticated:{deviceList[i].Authenticated}]");
                    logger.Trace($"Device {deviceList[i].DeviceName} {deviceList[i].DeviceAddress} [Connected:{deviceList[i].Connected}] [Authenticated:{deviceList[i].Authenticated}]");
                }
                Console.Write("Scelta: ");
                int choice = int.Parse(Console.ReadLine());

                BluetoothDeviceInfo device = deviceList[choice - 1];
                if (!device.Connected)
                {
                    //Guid serviceGuid = new Guid("00000000-0000-0000-0000-000000000000");
                    //Guid guid2 = new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
                    //localClient.Connect(device.DeviceAddress, guid2);
                    logger.Trace($"Trying to connect to {device.DeviceName}");
                    localClient.Connect(device.DeviceAddress, BluetoothService.SerialPort);
                    Console.Write($"{device.DeviceName} is now connected: {device.Connected}");
                    logger.Debug($"Device {device.DeviceName} connected.");
                    
                    /*
                    BluetoothEndPoint remoteEndPoint = new BluetoothEndPoint(device.DeviceAddress, BluetoothService.SerialPort);
                    BluetoothClient remoteClient = new BluetoothClient(remoteEndPoint);
                    remoteClient.Connect(localEndpoint);
                    stream = remoteClient.GetStream();
                    */
                }
                //stream = localClient.GetStream();
                logger.Debug($"Got localClient stream.");
            }
            catch(Exception ex)
            {
                if(ex as SocketException != null)
                    logger.Error($"SocketException Code: {((SocketException)ex).ErrorCode}");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
        }

        public string Read()
        {
            logger.Log(LogLevel.Trace, $"Reading");
            if(stream == null)
            {
                logger.Trace("Stream is null.");
                return null;
            }

            string ret = null;
            try
            {
                _bufferString = String.Empty;
                Array.Clear(_bufferChars, 0, BUFFER_SIZE);
                int readCount = 0;
                char c;

                do
                {
                    c = (char)stream.ReadByte();
                    logger.Log(LogLevel.Trace, $"Stream ReadByte {c} [count:{readCount + 1}]");

                    if (readCount < BUFFER_SIZE)
                        _bufferChars[readCount++] = c;
                    _bufferString += c;
                } while (c != '\r');
                ret = _bufferString;
                logger.Log(LogLevel.Info, $"Read ended {ret} [count:{readCount}]");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error while reading.");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
            return ret;
        }

        private BluetoothAddress GetLocalEndpointAddress()
        {
            // https://stackoverflow.com/questions/23431071/c-sharp-bluetooth-mac-address-of-our-adapter
            BluetoothRadio myRadio = BluetoothRadio.PrimaryRadio;
            if (myRadio == null)
            {
                return null;
            }
            return myRadio.LocalAddress;
        }
    }


    
    //public class BorlottoBT_MAC
    //{
    //    //private string BLUETOOTH_ENDPOINT_MAC = "";
    //    //private string BLUETOOTH_DEVICE_MAC = "";
    //    const int BUFFER_SIZE = 512;
    //    private string _bufferString;
    //    private char[] _bufferChars;

    //    private Logger logger = LogManager.GetCurrentClassLogger();
    //    private List<BluetoothDeviceInfo> deviceList = new List<BluetoothDeviceInfo>();
    //    private BluetoothAddress _endpointAddress;
    //    BluetoothEndPoint localEndpoint;
    //    BluetoothClient localClient;
    //    BluetoothComponent localComponent;
    //    AutoResetEvent discoverEnded;

    //    public BorlottoBT_MAC()
    //    {
    //        _bufferChars = new char[BUFFER_SIZE];
    //        _endpointAddress = GetEndpointAddress();
    //        discoverEnded = new AutoResetEvent(false);
            
    //        if(_endpointAddress == null)
    //        {
    //            logger.Log(LogLevel.Info, $"No bluetooth device found.");
    //            return;
    //        }

    //        logger.Log(LogLevel.Info, $"Local BluetoothAddress {_endpointAddress.ToString()}");


    //        // https://stackoverflow.com/questions/16802791/pair-bluetooth-devices-to-a-computer-with-32feet-net-bluetooth-library
    //        // mac is mac address of local bluetooth device
    //        localEndpoint = new BluetoothEndPoint(_endpointAddress, BluetoothService.SerialPort);
    //        // client is used to manage connections
    //        localClient = new BluetoothClient(localEndpoint);

    //        /*
    //        // component is used to manage device discovery
    //        localComponent = new BluetoothComponent(localClient);
    //        // async methods, can be done synchronously too
    //        localComponent.DiscoverDevicesProgress += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesProgress);
    //        localComponent.DiscoverDevicesComplete += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesComplete);
    //        localComponent.DiscoverDevicesAsync(255, true, true, true, true, null);

    //        discoverEnded.WaitOne();
    //        */

    //        deviceList = new List<BluetoothDeviceInfo>(localClient.DiscoverDevices());
    //        component_DiscoverDevicesComplete(null, null);

    //    }



    //    private BluetoothAddress GetEndpointAddress()
    //    {
    //        // https://stackoverflow.com/questions/23431071/c-sharp-bluetooth-mac-address-of-our-adapter
    //        BluetoothRadio myRadio = BluetoothRadio.PrimaryRadio;
    //        if (myRadio == null)
    //        {
    //            return null;
    //        }
    //        return myRadio.LocalAddress;
    //    }

    //    private void component_DiscoverDevicesProgress(object sender, DiscoverDevicesEventArgs e)
    //    {
    //        // log and save all found devices
    //        for (int i = 0; i < e.Devices.Length; i++)
    //        {
    //            logger.Trace($"Device discovered {e.Devices[i].DeviceName} [Address:{e.Devices[i].DeviceAddress}] [Remembered:{e.Devices[i].Remembered}] [Connected:{e.Devices[i].Connected}] [Authenticated:{e.Devices[i].Authenticated}]");
    //            this.deviceList.Add(e.Devices[i]);
    //        }
    //    }

    //    private void component_DiscoverDevicesComplete(object sender, DiscoverDevicesEventArgs e)
    //    {
    //        logger.Trace("Device Discover Completed");
    //        if(deviceList.Count == 0)
    //        {
    //            Console.WriteLine("No devices discovered.");
    //            SetDevice(-1);
    //            return;
    //        }

    //        Console.WriteLine("Available devices:");
    //        for (int i = 0; i < deviceList.Count; ++i)
    //            Console.WriteLine($"{i+1}\t {deviceList[i].DeviceName} {deviceList[i].DeviceAddress} [Connected:{deviceList[i].Connected}] [Authenticated:{deviceList[i].Authenticated}]");
    //        Console.Write("Scelta: ");
    //        try
    //        {
    //            SetDevice(int.Parse(Console.ReadLine()) - 1);
    //        }
    //        catch(Exception ex)
    //        {
    //            Console.WriteLine("Scelta non valida");
    //            SetDevice(-1);
    //        }
    //    }

    //    private void SetDevice(int index)
    //    {
    //        // device deve essere autenticated (e quindi prima anche paired)
    //        if (index >= 0 && index < deviceList.Count)
    //        {
    //            try
    //            {
    //                BluetoothDeviceInfo device = deviceList[index];
    //                if (!device.Connected)
    //                    localClient.Connect(device.DeviceAddress, BluetoothService.SerialPort);
    //                Read(localClient.GetStream());
    //            }
    //            catch(Exception ex)
    //            {
    //                //log
    //            }
    //        }
    //        End();
    //    }

    //    public string Read(NetworkStream stream)
    //    {
    //        logger.Log(LogLevel.Trace, $"Reading");
    //        string ret = null;
    //        try
    //        {
    //            _bufferString = String.Empty;
    //            Array.Clear(_bufferChars, 0, BUFFER_SIZE);
    //            int readCount = 0;
    //            char c;

    //            do
    //            {
    //                c = (char)stream.ReadByte();
    //                logger.Log(LogLevel.Trace, $"Stream ReadByte {c} [count:{readCount + 1}]");

    //                if (readCount < BUFFER_SIZE)
    //                    _bufferChars[readCount++] = c;
    //                _bufferString += c;

    //            } while (c != '\r');
    //            ret = _bufferString;
    //            logger.Log(LogLevel.Info, $"Read ended {ret} [count:{readCount}]");
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.Log(LogLevel.Error, $"Error while reading.");
    //            logger.Log(LogLevel.Error, "Message: " + ex.Message);
    //            logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
    //        }
    //        return ret;
    //    }

    //    private void End()
    //    {
    //        Console.WriteLine("Premere INVIO per terminare.");
    //        Console.ReadLine();
    //        discoverEnded.Set();
    //    }
    //}
    
}
