﻿using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Xtb.XApi.Records;
using Xtb.XApi.Streaming;
using Xtb.XApi.Utils;

namespace Xtb.XApi;

public class StreamingApiConnector : Connector
{
    private Task? _streamingReaderTask;

    /// <summary>
    /// Helper method to create a new instance based on address and port.
    /// </summary>
    /// <param name="address">Endpoint address.</param>
    /// <param name="port">Port for streaming data.</param>
    /// <param name="streamingListener">Streaming listener.</param>
    public static StreamingApiConnector Create(string address, int port, IStreamingListener? streamingListener = null)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(address), port);
        return new StreamingApiConnector(endpoint, streamingListener);
    }

    /// <summary>
    /// Dedicated streaming listener.
    /// </summary>
    private readonly IStreamingListener? _streamingListener;

    /// <summary>
    /// Creates new instance.
    /// </summary>
    /// <param name="endpoint">Endpoint for streaming data.</param>
    /// <param name="streamingListener">Streaming listener.</param>
    public StreamingApiConnector(IPEndPoint endpoint, IStreamingListener? streamingListener = null)
        : base(endpoint)
    {
        _streamingListener = streamingListener;
    }

    #region Events

    /// <summary>
    /// Event raised when a connection is established.
    /// </summary>
    public event EventHandler<EndpointEventArgs>? Connected;

    /// <summary>
    /// Event raised when a tick record is received.
    /// </summary>
    public event EventHandler<TickReceivedEventArgs>? TickReceived;

    /// <summary>
    /// Event raised when a trade record is received.
    /// </summary>
    public event EventHandler<TradeReceivedEventArgs>? TradeReceived;

    /// <summary>
    /// Event raised when a balance record is received.
    /// </summary>
    public event EventHandler<BalanceReceivedEventArgs>? BalanceReceived;

    /// <summary>
    /// Event raised when a trade status record is received.
    /// </summary>
    public event EventHandler<TradeStatusReceivedEventArgs>? TradeStatusReceived;

    /// <summary>
    /// Event raised when a profit record is received.
    /// </summary>
    public event EventHandler<ProfitReceivedEventArgs>? ProfitReceived;

    /// <summary>
    /// Event raised when a news record is received.
    /// </summary>
    public event EventHandler<NewsReceivedEventArgs>? NewsReceived;

    /// <summary>
    /// Event raised when a keep alive record is received.
    /// </summary>
    public event EventHandler<KeepAliveReceivedEventArgs>? KeepAliveReceived;

    /// <summary>
    /// Event raised when a candle record is received.
    /// </summary>
    public event EventHandler<CandleReceivedEventArgs>? CandleReceived;

    /// <summary>
    /// Event raised when read streamed message.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? StreamingErrorOccurred;

    #endregion Events

    /// <summary>
    /// Stream session id (member of login response). Should be set after the successful login.
    /// </summary>
    public string? StreamSessionId { get; set; }

    /// <summary>
    /// Connect to the streaming.
    /// </summary>
    public void Connect()
    {
        if (StreamSessionId == null)
        {
            throw new APICommunicationException("No session exists. Please login first.");
        }

        if (IsConnected)
        {
            throw new APICommunicationException("Stream already connected.");
        }

        ApiSocket = new TcpClient();
        var endpoint = Endpoint;
        ApiSocket.Connect(endpoint.Address, endpoint.Port);

        _apiConnected = true;

        Connected?.Invoke(this, new(endpoint));

        if (ShallUseSecureConnection)
        {
#pragma warning disable CA5359 // Do Not Disable Certificate Validation
            var callback = new RemoteCertificateValidationCallback(SslHelper.TrustAllCertificatesCallback);
#pragma warning restore CA5359 // Do Not Disable Certificate Validation
            var ssl = new SslStream(ApiSocket.GetStream(), false, callback);
            ssl.AuthenticateAsClient(endpoint.Address.ToString());
            StreamWriter = new StreamWriter(ssl);
            StreamReader = new StreamReader(ssl);
        }
        else
        {
            NetworkStream ns = ApiSocket.GetStream();
            StreamWriter = new StreamWriter(ns);
            StreamReader = new StreamReader(ns);
        }

        if (_streamingReaderTask == null)
        {
            CreateAndRunNewStreamingReaderTask(default);
        }
        else if (_streamingReaderTask.IsCompleted || _streamingReaderTask.IsFaulted || _streamingReaderTask.IsCanceled)
        {
            _streamingReaderTask = null;
            CreateAndRunNewStreamingReaderTask(default);
        }
    }

    /// <summary>
    /// Connect to the streaming.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (StreamSessionId == null)
        {
            throw new APICommunicationException("No session exists. Please login first.");
        }

        if (IsConnected)
        {
            throw new APICommunicationException("Stream already connected.");
        }

        ApiSocket = new TcpClient();
        var endpoint = Endpoint;
        try
        {
            await ApiSocket.ConnectAsync(endpoint.Address, endpoint.Port);
        }
        catch (OperationCanceledException)
        {
            throw new APICommunicationException("Connection attempt was canceled.");
        }

        _apiConnected = true;

        Connected?.Invoke(this, new(endpoint));

        if (ShallUseSecureConnection)
        {
#pragma warning disable CA5359 // Do Not Disable Certificate Validation
            var callback = new RemoteCertificateValidationCallback(SslHelper.TrustAllCertificatesCallback);
#pragma warning restore CA5359 // Do Not Disable Certificate Validation
            var ssl = new SslStream(ApiSocket.GetStream(), false, callback);
            await ssl.AuthenticateAsClientAsync(endpoint.Address.ToString());
            StreamWriter = new StreamWriter(ssl);
            StreamReader = new StreamReader(ssl);
        }
        else
        {
            var networkStream = ApiSocket.GetStream();
            StreamWriter = new StreamWriter(networkStream);
            StreamReader = new StreamReader(networkStream);
        }

        if (_streamingReaderTask == null)
        {
            CreateAndRunNewStreamingReaderTask(cancellationToken);
        }
        else if (_streamingReaderTask.IsCompleted || _streamingReaderTask.IsFaulted || _streamingReaderTask.IsCanceled)
        {
            _streamingReaderTask = null;
            CreateAndRunNewStreamingReaderTask(cancellationToken);
        }
    }

    private void CreateAndRunNewStreamingReaderTask(CancellationToken cancellationToken)
    {
        _streamingReaderTask = Task.Run(async () =>
        {
            while (IsConnected)
            {
                await ReadStreamMessageAsync(cancellationToken);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Reads stream message.
    /// </summary>
    private async Task ReadStreamMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await ReadMessageAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Incoming streaming message is null.");

            var responseBody = JsonNode.Parse(message)
                ?? throw new InvalidOperationException("Result of incoming parsed streaming message is null.");

            var commandName = (responseBody["command"]?.ToString())
                ?? throw new InvalidOperationException("Incoming streaming command is null.");

            var jsonSubnode = responseBody["data"]
                ?? throw new InvalidOperationException("Parsed json data object is null.");

            var jsonDataObject = jsonSubnode.AsObject();

            if (commandName == StreamingCommandName.TickPrices)
            {
                var tickRecord = new StreamingTickRecord();
                tickRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveTickRecordAsync(tickRecord, cancellationToken).ConfigureAwait(false);

                TickReceived?.Invoke(this, new(tickRecord));
            }
            else if (commandName == StreamingCommandName.Trade)
            {
                var tradeRecord = new StreamingTradeRecord();
                tradeRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveTradeRecordAsync(tradeRecord, cancellationToken).ConfigureAwait(false);

                TradeReceived?.Invoke(this, new(tradeRecord));
            }
            else if (commandName == StreamingCommandName.Balance)
            {
                var balanceRecord = new StreamingBalanceRecord();
                balanceRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveBalanceRecordAsync(balanceRecord, cancellationToken).ConfigureAwait(false);

                BalanceReceived?.Invoke(this, new(balanceRecord));
            }
            else if (commandName == StreamingCommandName.TradeStatus)
            {
                var tradeStatusRecord = new StreamingTradeStatusRecord();
                tradeStatusRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveTradeStatusRecordAsync(tradeStatusRecord, cancellationToken).ConfigureAwait(false);

                TradeStatusReceived?.Invoke(this, new(tradeStatusRecord));
            }
            else if (commandName == StreamingCommandName.Profit)
            {
                var profitRecord = new StreamingProfitRecord();
                profitRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveProfitRecordAsync(profitRecord, cancellationToken).ConfigureAwait(false);

                ProfitReceived?.Invoke(this, new(profitRecord));
            }
            else if (commandName == StreamingCommandName.News)
            {
                var newsRecord = new StreamingNewsRecord();
                newsRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveNewsRecordAsync(newsRecord, cancellationToken).ConfigureAwait(false);

                NewsReceived?.Invoke(this, new(newsRecord));
            }
            else if (commandName == StreamingCommandName.KeepAlive)
            {
                var keepAliveRecord = new StreamingKeepAliveRecord();
                keepAliveRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveKeepAliveRecordAsync(keepAliveRecord, cancellationToken).ConfigureAwait(false);

                KeepAliveReceived?.Invoke(this, new(keepAliveRecord));
            }
            else if (commandName == StreamingCommandName.Candle)
            {
                var candleRecord = new StreamingCandleRecord();
                candleRecord.FieldsFromJsonObject(jsonDataObject);

                if (_streamingListener != null)
                    await _streamingListener.ReceiveCandleRecordAsync(candleRecord, cancellationToken).ConfigureAwait(false);

                CandleReceived?.Invoke(this, new(candleRecord));
            }
            else
            {
                throw new APICommunicationException($"Unknown streaming record received. command:'{commandName}'");
            }
        }
        catch (APICommunicationException ex) when (ex.InnerException.InnerException is SocketException se)
        {
            if (se.ErrorCode != (int)SocketError.OperationAborted)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            OnStreamingErrorOccurred(ex);
        }
    }

    #region subscribe, unsubscribe

    public void SubscribePrice(string symbol, DateTimeOffset? minArrivalTime = null, int? maxLevel = null)
    {
        var tickPricesSubscribe = new TickPricesSubscribe(symbol, GetVerifiedSessionId(), minArrivalTime, maxLevel);
        WriteMessage(tickPricesSubscribe.ToString());
    }

    public void UnsubscribePrice(string symbol)
    {
        var tickPricesStop = new TickPricesStop(symbol);
        WriteMessage(tickPricesStop.ToString());
    }

    public void SubscribePrices(string[] symbols)
    {
        foreach (string symbol in symbols)
        {
            SubscribePrice(symbol);
        }
    }

    public void UnsubscribePrices(string[] symbols)
    {
        foreach (string symbol in symbols)
        {
            UnsubscribePrice(symbol);
        }
    }

    public void SubscribeTrades()
    {
        var tradeRecordsSubscribe = new TradeRecordsSubscribe(GetVerifiedSessionId());
        WriteMessage(tradeRecordsSubscribe.ToString());
    }

    public void UnsubscribeTrades()
    {
        var tradeRecordsStop = new TradeRecordsStop();
        WriteMessage(tradeRecordsStop.ToString());
    }

    public void SubscribeBalance()
    {
        var balanceRecordsSubscribe = new BalanceRecordsSubscribe(GetVerifiedSessionId());
        WriteMessage(balanceRecordsSubscribe.ToString());
    }

    public void UnsubscribeBalance()
    {
        var balanceRecordsStop = new BalanceRecordsStop();
        WriteMessage(balanceRecordsStop.ToString());
    }

    public void SubscribeTradeStatus()
    {
        var tradeStatusRecordsSubscribe = new TradeStatusRecordsSubscribe(GetVerifiedSessionId());
        WriteMessage(tradeStatusRecordsSubscribe.ToString());
    }

    public void UnsubscribeTradeStatus()
    {
        var tradeStatusRecordsStop = new TradeRecordsSubscribe(GetVerifiedSessionId());
        WriteMessage(tradeStatusRecordsStop.ToString());
    }

    public void SubscribeProfits()
    {
        var profitsSubscribe = new ProfitsSubscribe(GetVerifiedSessionId());
        WriteMessage(profitsSubscribe.ToString());
    }

    public void UnsubscribeProfits()
    {
        var profitsStop = new ProfitsStop();
        WriteMessage(profitsStop.ToString());
    }

    public void SubscribeNews()
    {
        var newsSubscribe = new NewsSubscribe(GetVerifiedSessionId());
        WriteMessage(newsSubscribe.ToString());
    }

    public void UnsubscribeNews()
    {
        var newsStop = new NewsStop();
        WriteMessage(newsStop.ToString());
    }

    public void SubscribeKeepAlive()
    {
        var keepAliveSubscribe = new KeepAliveSubscribe(GetVerifiedSessionId());
        WriteMessage(keepAliveSubscribe.ToString());
    }

    public void UnsubscribeKeepAlive()
    {
        var keepAliveStop = new KeepAliveStop();
        WriteMessage(keepAliveStop.ToString());
    }

    public void SubscribeCandles(string symbol)
    {
        var candleRecordsSubscribe = new CandleRecordsSubscribe(symbol, GetVerifiedSessionId());
        WriteMessage(candleRecordsSubscribe.ToString());
    }

    public void UnsubscribeCandles(string symbol)
    {
        var candleRecordsStop = new CandleRecordsStop(symbol);
        WriteMessage(candleRecordsStop.ToString());
    }

    public async Task SubscribePriceAsync(string symbol, DateTimeOffset? minArrivalTime = null, int? maxLevel = null, CancellationToken cancellationToken = default)
    {
        var tickPricesSubscribe = new TickPricesSubscribe(symbol, GetVerifiedSessionId(), minArrivalTime, maxLevel);
        await WriteMessageAsync(tickPricesSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribePriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var tickPricesStop = new TickPricesStop(symbol);
        await WriteMessageAsync(tickPricesStop.ToString(), cancellationToken);
    }

    public async Task SubscribePricesAsync(string[] symbols, CancellationToken cancellationToken = default)
    {
        foreach (string symbol in symbols)
        {
            await SubscribePriceAsync(symbol, cancellationToken: cancellationToken);
        }
    }

    public async Task UnsubscribePricesAsync(string[] symbols, CancellationToken cancellationToken = default)
    {
        foreach (string symbol in symbols)
        {
            await UnsubscribePriceAsync(symbol, cancellationToken);
        }
    }

    public async Task SubscribeTradesAsync(CancellationToken cancellationToken = default)
    {
        var tradeRecordsSubscribe = new TradeRecordsSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(tradeRecordsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeTradesAsync(CancellationToken cancellationToken = default)
    {
        var tradeRecordsStop = new TradeRecordsStop();
        await WriteMessageAsync(tradeRecordsStop.ToString(), cancellationToken);
    }

    public async Task SubscribeBalanceAsync(CancellationToken cancellationToken = default)
    {
        var balanceRecordsSubscribe = new BalanceRecordsSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(balanceRecordsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeBalanceAsync(CancellationToken cancellationToken = default)
    {
        var balanceRecordsStop = new BalanceRecordsStop();
        await WriteMessageAsync(balanceRecordsStop.ToString(), cancellationToken);
    }

    public async Task SubscribeTradeStatusAsync(CancellationToken cancellationToken = default)
    {
        var tradeStatusRecordsSubscribe = new TradeStatusRecordsSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(tradeStatusRecordsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeTradeStatusAsync(CancellationToken cancellationToken = default)
    {
        var tradeStatusRecordsStop = new TradeStatusRecordsStop();
        await WriteMessageAsync(tradeStatusRecordsStop.ToString(), cancellationToken);
    }

    public async Task SubscribeProfitsAsync(CancellationToken cancellationToken = default)
    {
        var profitsSubscribe = new ProfitsSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(profitsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeProfitsAsync(CancellationToken cancellationToken = default)
    {
        var profitsStop = new ProfitsStop();
        await WriteMessageAsync(profitsStop.ToString(), cancellationToken);
    }

    public async Task SubscribeNewsAsync(CancellationToken cancellationToken = default)
    {
        var newsSubscribe = new NewsSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(newsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeNewsAsync(CancellationToken cancellationToken = default)
    {
        var newsStop = new NewsStop();
        await WriteMessageAsync(newsStop.ToString(), cancellationToken);
    }

    public async Task SubscribeKeepAliveAsync(CancellationToken cancellationToken = default)
    {
        var keepAliveSubscribe = new KeepAliveSubscribe(GetVerifiedSessionId());
        await WriteMessageAsync(keepAliveSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeKeepAliveAsync(CancellationToken cancellationToken = default)
    {
        var keepAliveStop = new KeepAliveStop();
        await WriteMessageAsync(keepAliveStop.ToString(), cancellationToken);
    }

    public async Task SubscribeCandlesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var candleRecordsSubscribe = new CandleRecordsSubscribe(symbol, GetVerifiedSessionId());
        await WriteMessageAsync(candleRecordsSubscribe.ToString(), cancellationToken);
    }

    public async Task UnsubscribeCandlesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var candleRecordsStop = new CandleRecordsStop(symbol);
        await WriteMessageAsync(candleRecordsStop.ToString(), cancellationToken);
    }

    private string GetVerifiedSessionId()
    {
        if (StreamSessionId == null)
            throw new InvalidOperationException($"{nameof(StreamSessionId)} is null");

        return StreamSessionId;
    }

    #endregion subscribe, unsubscribe

    protected virtual void OnStreamingErrorOccurred(Exception ex)
    {
        var args = new ExceptionEventArgs(ex);
        StreamingErrorOccurred?.Invoke(this, args);

        if (!args.Handled)
        {
            // If the exception was not handled, rethrow it
            throw new APICommunicationException("Read streaming message failed.", ex);
        }
    }

    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            base.Dispose(disposing);
            StreamSessionId = null!;

            _disposed = true;
        }
    }

    ~StreamingApiConnector()
    {
        Dispose(false);
    }
}