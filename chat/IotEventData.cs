using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class IotEventData : EventArgs {

    public int State { get; }
    public string Id { get; }

    public IotEventData(string id, int state) {
        State = state;
        Id = id;
    }
    public IotEventData(string id, bool boolState) {
        State = boolState ? 1 : 0;
        Id = id;
    }

    public bool StateToBool() {
        return State == 1;
    }

    public string DataToString() {
        return $"{Id}:{State}";
    }

    public static IotEventData? StringToData(string data) {


        Console.WriteLine($"{data}");

        // Split the string by ':' and trim any spaces
        string[] parts = data.Split(':');

        if(parts.Length != 2) { return null; }

        // Extract the id part
        string id = parts[0];

        // Try to convert the second part to an integer
        if (int.TryParse(parts[1].Trim(), out int state)) {

            Console.WriteLine("ID: " + id);
            Console.WriteLine("State: " + state);

        } else {
            Console.WriteLine("Error: Unable to parse the state part to an integer.");
            return null;
        }

        return new IotEventData(id, state);
    }
}
