using BSICK.Sensors.LMS1xx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sick_test
{
    class Program
    {
        static void Main(string[] args)
        {


            // connect to real sensor and write incoming datastream to file
            //using var lms = new LMS1XX("192.168.5.241", 2112, 5000, 5000, $"dump-{DateTime.Now:yyyyMMdd-HHmmss}.bin");

            // use file as sensor responce
            using var lms = new LMS1XX("dump-20210720-172823.bin");

            lms.Connect();
            Console.WriteLine($"QueryStatus {lms.QueryStatus()}");
            //Console.WriteLine($"SetAccessMode {lms.SetAccessMode()}");
            //Console.WriteLine($"Reboot {lms.Reboot()}");
            //while (true)
            //{
            //    try
            //    {
            //        var status = lms.QueryStatus();
            //        break;
            //    }
            //    catch (Exception ex)
            //    {
            //        lms.Disconnect();
            //        Thread.Sleep(100);
            //        lms.Connect();
            //    }
            //}
            Console.WriteLine($"SetAccessMode {lms.SetAccessMode()}");
            Console.WriteLine($"QueryStatus {lms.QueryStatus()}");
            Console.WriteLine($"Stop {lms.Stop()}");
            Console.WriteLine($"QueryStatus {lms.QueryStatus()}");

            Console.WriteLine($"Start {lms.Start()}");
            Console.WriteLine($"QueryStatus {lms.QueryStatus()}");
            Console.WriteLine($"Run {lms.Run()}");
            Console.WriteLine($"QueryStatus {lms.QueryStatus()}");
            while (true)
            {
                var statResult = lms.QueryStatus();
                if (statResult.Contains("STlms 7"))
                {
                    Console.WriteLine($"Started {statResult}");
                    break;
                }
                Console.WriteLine($"WAITING STATUS {statResult}");
                Thread.Sleep(100);

            }
            Console.WriteLine($"StartContinuous {lms.StartContinuous()}");

            try
            {
                var isWorking = true;
                var prevCnt = 0;
                var badDataCnt = 0;
                var lostSync = 0;
                var prevScan = 0;
                var prevTime = 0;
                var totalCnt = 0;
                var prevTotal = 0;
                var localCnt = 0;

                while (isWorking)
                {
                    bool needLog = false;
                    totalCnt++;
                    localCnt++;
                    LMDScandataResult data;
                    try
                    {
                        data = lms.ScanContinious();
                    }
                    catch (EndOfStreamException ex)
                    {
                        Console.WriteLine("End of stream");
                        break;
                    }
                    

                    if (data.ScanCounter == null || data.ScanCounter != prevScan)
                    {
                        lostSync++;
                        needLog = true;
                    }

                    if (data == null
                        || data.DistancesData == null
                        || data.DistancesData.Count <= 0
                        || data.IsError == true)
                    {
                        badDataCnt++;
                        needLog = true;
                    }

                    if (true)
                    {
                        Console.WriteLine($"{DateTime.Now:HH.mm.ss.fff}:{data.TimeOfTransmission.GetValueOrDefault()} " +
                            $"Total = {totalCnt}:{totalCnt-prevTotal}:{localCnt} " +
                            $"Bads = {badDataCnt} " +
                            $"DeSync = {lostSync}:{prevScan}:{data?.ScanCounter}:{data?.ScanCounter.GetValueOrDefault() - prevScan} " +
                            $"Status = {data?.DeviceStatus} " +
                            $"Scans = {data?.ScanCounter} " +
                            $"Telegrams = {data?.TelegramCounter} " +
                            $"Delta = {data.ScanCounter.GetValueOrDefault() - data.TelegramCounter.GetValueOrDefault()} " +
                            $"bytes = {data?.DistancesData?.Count}");
                        prevTotal = totalCnt;
                        localCnt = 0;
                    }

                    prevScan = (data?.ScanCounter ?? 0) + 1;
                    prevTime = (int)(data?.TimeOfTransmission ?? 0);

                    if (Console.KeyAvailable)
                        isWorking = false;
                }
            }
            finally
            {
                if (!lms.needEmu)
                {
                    //lms.Flush();
                    var stopContResult = lms.StopContinuous();
                    //lms.Flush();
                    var accessResult2 = lms.SetAccessMode();
                    var stopResult = lms.Stop();
                    var s1 = lms.QueryStatus();
                    Console.WriteLine(s1);
                }
                lms.Disconnect();
                lms.Dispose();
            }
        }
    }
}
