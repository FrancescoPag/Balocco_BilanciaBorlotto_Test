using Bilancia_Test;
using Borlotto_Test;
using InTheHand.Net.Bluetooth;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Balocco_BilanciaBorlotto_Test
{
    class Program
    {
        const int TIMER = 500;

        static void Main(string[] args)
        {

            



            Console.WriteLine("Programma di test per la lettura dei valori inviati dalla bilancia e dal distanziometro");
            int choice = -1;
            while (true)
            {
                Console.WriteLine("Test su:\n\t1. Bilancia\n\t2. Distanziometro - porta COM\n\t3. Distanziometro - Bluetooth");
                Console.Write("Inserire valore: ");
                int.TryParse(Console.ReadLine(), out choice);
                if (choice == 1 || choice == 2 || choice == 3)
                    break;
                else
                    Console.WriteLine("Valore non valido.");
            }

            switch(choice)
            {
                case 1: Bilancia(); break;
                case 2: BorlottoCOM(); break;
                case 3: BorlottoBT(); break;
                default: break;
            }         
        }

        static void Bilancia()
        {
            Console.WriteLine("Porte disponibili:");
            foreach (string s in SerialPort.GetPortNames())
                Console.WriteLine($"\t- {s}");
            Console.Write("Inserire valore: ");
            string portName = Console.ReadLine();

            BilanciaReader br = new BilanciaReader(portName);
            for(int i = 1; i < 6; ++i)
            {
                Console.WriteLine($"BILANCIA\tLettura {i}: {br.Read()}");
                Thread.Sleep(TIMER);
            }

            Console.WriteLine("Premere INVIO per terminare.");
            Console.ReadLine();
        }

        static void BorlottoCOM()
        {
            Console.WriteLine("Porte disponibili:");
            foreach (string s in SerialPort.GetPortNames())
                Console.WriteLine($"\t- {s}");
            Console.Write("Inserire valore: ");
            string portName = Console.ReadLine();

            BorlottoReaderCom br = new BorlottoReaderCom(portName);
            br.Open();
            for (int i = 1; i < 6; ++i)
            {
                Console.WriteLine($"BORLOTTO_COM\tLettura {i}: {br.Read()}");
                Thread.Sleep(TIMER);
            }
            br.Close();

            Console.WriteLine("Premere INVIO per terminare.");
            Console.ReadLine();
        }

        static void BorlottoBT()
        {
            BorlottoReaderBluetooth br = new BorlottoReaderBluetooth();

            for (int i = 1; i < 6; ++i)
            {
                Console.WriteLine($"BORLOTTO_BLUETOOTH\tLettura {i}: {br.Read()}");
                Thread.Sleep(TIMER);
            }

            Console.WriteLine("Premere INVIO per terminare.");
            Console.ReadLine();
        }
    }
}
