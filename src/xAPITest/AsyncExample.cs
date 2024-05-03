﻿using System;
using System.Linq;
using xAPI.Sync;
using xAPI.Responses;
using xAPI.Commands;
using xAPI.Records;
using xAPI.Codes;
using System.Threading.Tasks;

namespace xAPITest
{
    public static class AsyncExample
    {
        public static async Task Run(Server serverData, string userId, string password)
        {
            Console.WriteLine("Server address: " + serverData.Address + " port: " + serverData.MainPort + " streaming port: " + serverData.StreamingPort);

            // Connect to server
            SyncAPIConnector connector = null;
            try
            {
                connector = new SyncAPIConnector(serverData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(1);
            }

            Console.WriteLine("Connected to the server");

            // Login to server
            Credentials credentials = new Credentials(userId, password, "", "YOUR APP NAME");

            LoginResponse loginResponse = await APICommandFactory.ExecuteLoginCommandAsync(connector, credentials, true);
            Console.WriteLine("Logged in as: " + userId);

            var pingResponse = await APICommandFactory.ExecutePingCommandAsync(connector, true);
            Console.WriteLine("Ping status: " + pingResponse.Status);

            // Execute GetServerTime command
            ServerTimeResponse serverTimeResponse = await APICommandFactory.ExecuteServerTimeCommandAsync(connector, true);
            Console.WriteLine("Server time: " + serverTimeResponse.TimeString);

            // Execute GetAllSymbols command
            AllSymbolsResponse allSymbolsResponse = await APICommandFactory.ExecuteAllSymbolsCommandAsync(connector, true);
            Console.WriteLine("All symbols count: " + allSymbolsResponse.SymbolRecords.Count);

            // Print first 5 symbols
            Console.WriteLine("First five symbols:");
            foreach (SymbolRecord symbolRecord in allSymbolsResponse.SymbolRecords.Take(5))
            {
                Console.WriteLine(" > " + symbolRecord.Symbol + " ask: " + symbolRecord.Ask + " bid: " + symbolRecord.Bid);
            }

            // get US500 info
            Console.WriteLine("Getting US500 symbol.");
            var us500Symbol = await APICommandFactory.ExecuteSymbolCommandAsync(connector, "US500");
            Console.WriteLine(us500Symbol.Symbol.Symbol + " ask: " + us500Symbol.Symbol.Ask + " bid: " + us500Symbol.Symbol.Bid);

            Console.WriteLine("\n----Trading----");

            var us500TradeTransInfo = new TradeTransInfoRecord(
                TRADE_OPERATION_CODE.BUY,
                TRADE_TRANSACTION_TYPE.ORDER_OPEN,
                us500Symbol.Symbol.Ask,
                null,
                null,
                us500Symbol.Symbol.Symbol,
                0.1,
                null,
                null,
                null);

            await Task.Delay(500);

            // Warning: Opening trade. Make sure you have set up demo account!
            TradeTransactionResponse us500TradeTransaction = await APICommandFactory.ExecuteTradeTransactionCommandAsync(connector, us500TradeTransInfo, true);
            Console.WriteLine($"Opened position. result order:{us500TradeTransaction.Order}");

            await Task.Delay(1000);

            // get all open trades
            TradesResponse openTrades = await APICommandFactory.ExecuteTradesCommandAsync(connector, true, true);
            Console.WriteLine("Open positions: ");
            foreach (var tradeRecord in openTrades.TradeRecords)
            {
                Console.WriteLine($" > o:{tradeRecord.Order}, o2:{tradeRecord.Order2}, pos:{tradeRecord.Position}, symbol:{tradeRecord.Symbol}, " +
                    $"open price:{tradeRecord.Open_price}, profit:{tradeRecord.Profit}, tp:{tradeRecord.Tp}");
            }

            TradeRecord us500trade = openTrades.TradeRecords.First(t => t.Symbol == "US500");

            await Task.Delay(1000);

            // update trade transaction
            us500TradeTransInfo.Order = us500trade.Order;
            us500TradeTransInfo.Tp = us500trade.Open_price + 200;
            //us500TradeTransInfo.CustomComment = "my custom comment";
            TradeTransactionResponse updatedUs500TradeTransaction = await APICommandFactory.ExecuteTradeTransactionCommandAsync(connector, us500TradeTransInfo, true);
            Console.WriteLine($"Modified position. order:{us500trade.Order} -> tp:{us500TradeTransInfo.Tp}, result order:{updatedUs500TradeTransaction.Order}");

            await Task.Delay(2000);

            TradesResponse openTrades2 = await APICommandFactory.ExecuteTradesCommandAsync(connector, true, true);
            Console.WriteLine("Open positions: ");
            foreach (var tradeRecord in openTrades2.TradeRecords)
            {
                Console.WriteLine($" > o:{tradeRecord.Order}, o2:{tradeRecord.Order2}, pos:{tradeRecord.Position}, symbol:{tradeRecord.Symbol}, " +
                    $"open price:{tradeRecord.Open_price}, profit:{tradeRecord.Profit}, tp:{tradeRecord.Tp}");
            }

            await Task.Delay(1000);

            // close trade transaction
            us500TradeTransInfo.Type = TRADE_TRANSACTION_TYPE.ORDER_CLOSE;
            us500TradeTransInfo.Price = us500Symbol.Symbol.Bid;
            TradeTransactionResponse closedUs500TradeTransaction = await APICommandFactory.ExecuteTradeTransactionCommandAsync(connector, us500TradeTransInfo, true);
            Console.WriteLine($"Closed position. order:{us500TradeTransInfo.Order}, result order:{closedUs500TradeTransaction.Order}");

            await Task.Delay(1000);

            TradesResponse openTrades3 = await APICommandFactory.ExecuteTradesCommandAsync(connector, true, true);
            if (openTrades3.TradeRecords.Count != 0)
            {
                Console.WriteLine("Open positions: ");
                foreach (var tradeRecord in openTrades3.TradeRecords)
                {
                    Console.WriteLine($" > o:{tradeRecord.Order}, o2:{tradeRecord.Order2}, pos:{tradeRecord.Position}, symbol:{tradeRecord.Symbol}, " +
                        $"open price:{tradeRecord.Open_price}, profit:{tradeRecord.Profit}, tp:{tradeRecord.Tp}");
                }
            }

            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
