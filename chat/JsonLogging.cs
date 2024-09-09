using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public class Logger {
    private readonly string _filePath;
    private readonly List<LogEntry> _logEntries;

    public Logger(string filePath) {
        _filePath = filePath;
        _logEntries = new List<LogEntry>();

        // If file already exists, load existing log entries
        if (File.Exists(_filePath)) {
            var existingContent = File.ReadAllText(_filePath);
            if (!string.IsNullOrWhiteSpace(existingContent)) {
                _logEntries = JsonConvert.DeserializeObject<List<LogEntry>>(existingContent);
            }
        }
    }

    public void Log(string message) {
        var logEntry = new LogEntry {
            Message = message
        };

        _logEntries.Add(logEntry);
        WriteLogEntriesToFile();
    }

    private void WriteLogEntriesToFile() {
        var json = JsonConvert.SerializeObject(_logEntries, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }

    public class LogEntry {
        public string Message { get; set; }
    }

    public static string GetCurrentTimeStamp() {
        return $"{DateTime.UtcNow:HH:mm:ss.fffffff}";
    }
}

