﻿using System;
using XApi.Codes;

namespace XApi;

public interface ITradeRecord : ISymbol, IPosition
{
    double? ClosePrice { get; }

    DateTimeOffset? CloseTime { get; }

    bool? Closed { get; }

    TRADE_OPERATION_TYPE? TradeOperation { get; }

    string? Comment { get; }

    double? Commission { get; }

    string? CustomComment { get; }

    int? Digits { get; }

    DateTimeOffset? ExpirationTime { get; }

    double? MarginRate { get; }

    double? OpenPrice { get; }

    DateTimeOffset? OpenTime { get; }

    double? Profit { get; }

    double? Sl { get; }

    double? Storage { get; }

    double? Tp { get; }

    double? Volume { get; }
}