using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilancia_Test
{
    public class BilanciaReader
    {
        const int BUFFER_SIZE = 33;
        const int BAUD_RATE = 9600;
        const Parity PARITY_BIT = Parity.None;
        const int DATA_BITS = 8;
        const StopBits STOP_BITS = StopBits.One;
        const string START_COMMAND = "#01A\r";

        private char[] _buffer;
        private SerialPort _port;
        private Logger logger = LogManager.GetCurrentClassLogger();

       public BilanciaReader(string comPort)
        {
            _port = new SerialPort(comPort, BAUD_RATE, PARITY_BIT, DATA_BITS, STOP_BITS);
            _buffer = new char[BUFFER_SIZE + 1];
            logger.Log(LogLevel.Trace, $"SerialPort: {comPort}, {BAUD_RATE}, {PARITY_BIT}, {DATA_BITS}, {STOP_BITS}");
        }

        public string Read()
        {
            string ret = null;
            logger.Log(LogLevel.Trace, "Starting read");
            try
            {
                _port.Open();
                logger.Log(LogLevel.Debug, "Port open");
                Array.Clear(_buffer, 0, BUFFER_SIZE + 1);

                int readCount = 0;
                _port.Write(START_COMMAND);
                logger.Log(LogLevel.Debug, $"Port write {START_COMMAND}");

                while (readCount < BUFFER_SIZE && _buffer[readCount] != '\r')
                {
                    _buffer[readCount++] = (char)_port.ReadChar();
                    logger.Log(LogLevel.Debug, $"Port readChar {_buffer[readCount-1]} [count:{readCount}]");
                }

                ret = new string(_buffer, 0, readCount);
                logger.Log(LogLevel.Debug, $"Read ended: {ret} [count:{readCount}]");
                

                _port.Close();
                logger.Log(LogLevel.Debug, "Port closed");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Error while reading");
                logger.Log(LogLevel.Error, "Message: " + ex.Message);
                logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
            }
            finally
            {
                try
                {
                    if (_port.IsOpen)
                    {
                        logger.Log(LogLevel.Debug, "Finally block, port is still open");
                        _port.Close();
                        logger.Log(LogLevel.Debug, "Finally block, Port closed");
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "Finally block, error while closing the port!");
                    logger.Log(LogLevel.Error, "Message: " + ex.Message);
                    logger.Log(LogLevel.Error, "StackTrace " + ex.StackTrace);
                }
            }
            return ret;
        }

    }
}
