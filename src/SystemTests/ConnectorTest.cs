﻿using System;

namespace Xtb.XApi.SystemTests;

public sealed class ConnectorTest : TestBase
{
    public ConnectorTest(Connector connector, string user, string password)
        : base(user, password)
    {
        Client = connector;
    }

    public Connector Client { get; set; }

    public void Run()
    {
        ConnectionStage();
    }

    public void ConnectionStage()
    {
        Stage("Connection");

        Action($"Establishing connection");
        try
        {
            Client.Connect();
            Pass();
        }
        catch (Exception ex)
        {
            Fail(ex, true);
        }

        Action("Ping");
        try
        {
            var response = Client.SendMessageWaitResponse(pingRequest);
            Pass(response);
        }
        catch (Exception ex)
        {
            Fail(ex);
        }

        Action($"Dropping connection");
        try
        {
            Client.Disconnect();
            Pass();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }

        Action($"Reestablishing connection");
        try
        {
            Client.Connect();
            Pass();
        }
        catch (Exception ex)
        {
            Fail(ex, true);
        }

        Action("Ping");
        try
        {
            var response = Client.SendMessageWaitResponse(pingRequest);
            Pass(response);
        }
        catch (Exception ex)
        {
            Fail(ex);
        }

        Action("Getting version");
        try
        {
            var response = Client.SendMessageWaitResponse(versionRequest);
            Pass(response);
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private string pingRequest =
        $$"""
        {
            "command": "ping",
            "pretyPrint": false,
            "arguments": {},
            "customTag": "1"
        }
        """;

    private string versionRequest =
        $$"""
        {
            "command": "getVersion",
            "pretyPrint": null,
            "arguments": {},
            "customTag": "2"
        }
        """;
}