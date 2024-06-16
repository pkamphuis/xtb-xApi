using System.Collections.Generic;
using System.Text.Json.Nodes;
using xAPI.Records;
using System.Linq;
using System.Diagnostics;

namespace xAPI.Responses
{
    [DebuggerDisplay("trades:{TradeRecords.Count}")]
    public class TradesHistoryResponse : BaseResponse
    {
        public TradesHistoryResponse()
            : base()
        { }

        public TradesHistoryResponse(string body)
            : base(body)
        {
            if (ReturnData is null)
                return;

            var arr = ReturnData.AsArray();
            foreach (JsonObject e in arr.OfType<JsonObject>())
            {
                var record = new TradeRecord();
                record.FieldsFromJsonObject(e);
                TradeRecords.AddLast(record);
            }
        }

        public LinkedList<TradeRecord> TradeRecords { get; init; } = [];
    }
}