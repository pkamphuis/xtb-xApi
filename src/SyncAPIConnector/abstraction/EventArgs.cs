using System;
using System.Net;
using Xtb.XApi.Records;

namespace Xtb.XApi;

public class ExceptionEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;

    public bool Handled { get; set; }
}

public class MessageEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public class CommandEventArgs(ICommand command) : EventArgs
{
    public ICommand Command { get; } = command;
}

public class EndpointEventArgs(IPEndPoint endpoint) : EventArgs
{
    public IPEndPoint EndPoint { get; } = endpoint;
}

public class TickReceivedEventArgs(StreamingTickRecord tickRecord) : EventArgs
{
    public StreamingTickRecord TickRecord { get; } = tickRecord;
}

public class TradeReceivedEventArgs(StreamingTradeRecord tradeRecord) : EventArgs
{
    public StreamingTradeRecord TradeRecord { get; } = tradeRecord;
}

public class BalanceReceivedEventArgs(StreamingBalanceRecord balanceRecord) : EventArgs
{
    public StreamingBalanceRecord BalanceRecord { get; } = balanceRecord;
}

public class TradeStatusReceivedEventArgs(StreamingTradeStatusRecord tradeStatusRecord) : EventArgs
{
    public StreamingTradeStatusRecord TradeStatusRecord { get; } = tradeStatusRecord;
}

public class ProfitReceivedEventArgs(StreamingProfitRecord profitRecord) : EventArgs
{
    public StreamingProfitRecord ProfitRecord { get; } = profitRecord;
}

public class NewsReceivedEventArgs(StreamingNewsRecord newsRecord) : EventArgs
{
    public StreamingNewsRecord NewsRecord { get; } = newsRecord;
}

public class KeepAliveReceivedEventArgs(StreamingKeepAliveRecord keepAliveRecord) : EventArgs
{
    public StreamingKeepAliveRecord KeepAliveRecord { get; } = keepAliveRecord;
}

public class CandleReceivedEventArgs(StreamingCandleRecord candleRecord) : EventArgs
{
    public StreamingCandleRecord CandleRecord { get; } = candleRecord;
}