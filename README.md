# KrakenIPC
Provides a simple IPC over named pipes, easy to use, based on RPC.

Usage:

Define a common interface (Contract), common between the client and server:

```C#
//Service interface, that will be implemented on the server's side
public interface IMyService
    {
        string GetData(int value);
        CompositeType GetDataUsingDataContract(CompositeType composite);
    }

//An object model to be sent either as a parameter and/or return type
public class CompositeType
    {
        public bool BoolValue { get; set; } = true;
        public string StringValue { get; set; } = "Hello";
    }
```

Implement the interface on the server side:
```C#
public class MyService : IMyService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }
        
        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
    }
```

Start the server:
```C#
var pipeServer = new PipeServer<MyService, IMyService>();
```

Client code to make the call:
```C#
var pipeClient = new PipeClient<IMyService>();
var proxy = pipeClient.GetProxy();

string data = proxy.GetData(3); //returns "You entered: 3"
CompositeType composite = proxy.GetDataUsingDataContract(new CompositeType()); //composite.StringValue will be "Hello Suffix"
```
