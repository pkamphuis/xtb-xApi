﻿using System.Diagnostics;
using System.Text.Json.Nodes;
using xAPI.Codes;

namespace xAPI.Records;

[DebuggerDisplay("o:{Order}, price:{Price}")]
public record StreamingTradeStatusRecord : IBaseResponseRecord
{
    public string? CustomComment { get; set; }

    public string? Message { get; set; }

    public long? Order { get; set; }

    public double? Price { get; set; }

    public REQUEST_STATUS? RequestStatus { get; set; }

    public void FieldsFromJsonObject(JsonObject value)
    {
        CustomComment = (string?)value["customComment"];
        Message = (string?)value["message"];
        Order = (long?)value["order"];
        Price = (double?)value["price"];

        var requestStatus = (long?)value["requestStatus"];
        RequestStatus = requestStatus.HasValue ? new REQUEST_STATUS(requestStatus.Value) : null;
    }
}