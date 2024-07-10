﻿using System.Text.Json.Nodes;

namespace xAPI.Records
{
    public record RedirectRecord : IBaseResponseRecord
    {
        public int MainPort { get; set; }

        public int StreamingPort { get; set; }

        public string? Address { get; set; }

        public void FieldsFromJsonObject(JsonObject value)
        {
            MainPort = (int)value["mainPort"];
            StreamingPort = (int)value["streamingPort"];
            Address = (string?)value["address"];
        }

        public override string ToString()
        {
            return "RedirectRecord [" +
                "mainPort=" + this.MainPort +
                ", streamingPort=" + this.StreamingPort +
                ", address=" + this.Address + "]";
        }
    }
}