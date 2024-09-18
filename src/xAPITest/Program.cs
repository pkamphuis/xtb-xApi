﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Xtb.XApi.SystemTests;

internal static class Program
{
    public const int DemoRequestingPort = 5124;
    public const int DemoStreamingPort = 5125;
    public const int RealRequestingPort = 5112;
    public const int RealStreamingPort = 5113;

    public static IPAddress Address => IPAddress.Parse("81.2.190.163");
    public static IPEndPoint DemoRequestingEndpoint => new(Address, DemoRequestingPort);
    public static IPEndPoint DemoStreamingEndpoint => new(Address, DemoStreamingPort);
    public static IPEndPoint RealRequestingEndpoint => new(Address, RealRequestingPort);
    public static IPEndPoint RealStreamingEndpoint => new(Address, RealStreamingPort);

    private static string _userId = "16697884";
    private static string _password = "xoh11724";

    private static void Main(string[] args)
    {
        RunSyncTest();
        RunAsyncTest();

        Console.WriteLine("Done.");
        Console.Read();
    }

    private static void RunSyncTest()
    {
        using (var client = new XApiClient(DemoRequestingEndpoint, DemoStreamingEndpoint))
        {
            Console.WriteLine("----Sync test---");
            var syncTest = new SyncTest(client, _userId, _password, @"\messages\");
            syncTest.Run();
        }
    }

    private static void RunAsyncTest()
    {
        using (var apiConnector = new XApiClient(DemoRequestingEndpoint, DemoStreamingEndpoint))
        {
            Console.WriteLine();
            Console.WriteLine("----Async test---");
            Console.WriteLine("(esc) abort");
            var asyncTest = new AsyncTest(apiConnector, _userId, _password);
            using var tokenSource = new CancellationTokenSource();

            var keyWaitTask = Task.Run(() =>
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            tokenSource.Cancel();
                            Console.WriteLine("Operation aborted.");
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            });

            try
            {
                asyncTest.RunAsync(tokenSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was canceled.");
            }
            finally
            {
                tokenSource.Cancel();
                keyWaitTask.Wait();
            }
        }
    }
}