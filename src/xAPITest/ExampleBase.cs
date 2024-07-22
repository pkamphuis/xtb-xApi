﻿using System;
using System.Threading.Tasks;
using xAPI.Responses;

namespace xAPITest;

public abstract class ExampleBase
{
    public bool ShallOpenTrades { get; set; }

    protected static void Stage(string name)
    {
        var oc = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine($"Stage: {name}");

        Console.ForegroundColor = oc;
    }

    protected static void Action(string name)
    {
        Task.Delay(200);
        Console.Write($"  {name}...");
    }

    protected static void Pass(BaseResponse? response = null)
    {
        var oc = Console.ForegroundColor;

        if (response is null || response.Status == true)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("OK");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.WriteLine($"Error: {response.ErrCode}, {response.ErrorDescr}");
        }

        Console.ForegroundColor = oc;
    }

    protected static void Fail(Exception ex, bool interrupt = false)
    {
        var oc = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine($"Fail: {ex.Message}");

        Console.ForegroundColor = oc;

        if (interrupt)
            Environment.Exit(1);
    }

    protected static void Detail(string? text)
    {
        var oc = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;

        Console.WriteLine($"    {text}");

        Console.ForegroundColor = oc;
    }
}