﻿using System.Text.Json.Nodes;
using XApi.Commands;

namespace XApi.Streaming;

internal sealed class BalanceRecordsStop : ICommand
{
    public const string Name = "stopBalance";

    public string CommandName => Name;

    public override string ToString()
    {
        JsonObject result = new()
        {
            { "command", CommandName }
        };

        return result.ToJsonString();
    }
}