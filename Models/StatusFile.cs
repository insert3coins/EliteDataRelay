 ﻿using System.Text.Json.Serialization;
 
 namespace EliteDataRelay.Models
 {
     /// <summary>
     /// Represents the structure of the Status.json file.
     /// </summary>
     public class StatusFile
     {
         [JsonPropertyName("timestamp")]
         public string? Timestamp { get; set; }
 
         [JsonPropertyName("event")]
         public string? Event { get; set; }
 
         [JsonPropertyName("Balance")]
        public long? Balance { get; set; }
     }
 }