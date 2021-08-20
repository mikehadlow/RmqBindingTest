This repository provides a .NET solution to test large numbers of bindings on RabbitMQ.

Accompanying blog post is [here](https://mikehadlow.com/posts/large-numbers-of-bindings-with-rabbitmq/)

To build simply clone this repository. The `RmqBindingTest.sln` solution file should open with Visual Studio and restore and build without any further configuration.

To build self contained executables use the `dotnet` command as follows:
```
dotnet publish -o Output --self-contained -r win-x64
```

There are three console projects, 

* `RmqBindingTest.Consumer` - This creates a single exclusive queue and the number of bindings specified (see below). 
* `RmqBindingTest.Publisher` - This publishes messages with a range of routing keys as specified (see below).
* `RmqBindingTest.StatsCollector` - Counts the number of messages published and consumed as the publishers and consumers exit.

The console applications require the following environment variables to be set:
```
BINDING_TEST_RMQ_HOST
BINDING_TEST_RMQ_VHOST
BINDING_TEST_RMQ_USER
BINDING_TEST_RMQ_PASSWORD
```
I ran multiple instances of the publisher and consumer on two separate VMs using the following batch scripts:

For the publisher, to launch 10 instances, each publishing messages with routing keys from "00.0.0" to "99.9.9":
```bat
pushd <location of RmqBindingTest.Publisher.exe>

start "" "RmqBindingTest.Publisher.exe" A0 0000 9999
start "" "RmqBindingTest.Publisher.exe" A1 0000 9999
start "" "RmqBindingTest.Publisher.exe" A2 0000 9999
start "" "RmqBindingTest.Publisher.exe" A3 0000 9999
start "" "RmqBindingTest.Publisher.exe" A4 0000 9999
start "" "RmqBindingTest.Publisher.exe" A5 0000 9999
start "" "RmqBindingTest.Publisher.exe" A6 0000 9999
start "" "RmqBindingTest.Publisher.exe" A7 0000 9999

popd
```

For the consumer, to launch 10 instances each with its own exclusive queue and 1000 bindings from "n0.0.0" to "n9.9.9":
```bat
pushd <location of RmqBindingTest.Consumer.exe>

start "" "RmqBindingTest.Consumer.exe" A0 0000 0999
start "" "RmqBindingTest.Consumer.exe" A1 1000 1999
start "" "RmqBindingTest.Consumer.exe" A2 2000 2999
start "" "RmqBindingTest.Consumer.exe" A3 3000 3999
start "" "RmqBindingTest.Consumer.exe" A4 4000 4999
start "" "RmqBindingTest.Consumer.exe" A5 5000 5999
start "" "RmqBindingTest.Consumer.exe" A6 6000 6999
start "" "RmqBindingTest.Consumer.exe" A7 7000 7999
start "" "RmqBindingTest.Consumer.exe" A8 8000 8999
start "" "RmqBindingTest.Consumer.exe" A9 9000 9999

popd
```

Before exiting any of the publishers or consumers (exit the publishers first), launch the `RmqBindingTest.StatsCollector.exe`. 
This will report the total number of messages published and consumed. The output will look something like this:
```
Stats consumer started. Ctrl-C to exit.
PUB:      10461, CON:          0, MSG: PUB|A7|10461
PUB:      21090, CON:          0, MSG: PUB|A6|10629
PUB:      31842, CON:          0, MSG: PUB|A5|10752
PUB:      42701, CON:          0, MSG: PUB|A4|10859
PUB:      53663, CON:          0, MSG: PUB|A3|10962
PUB:      64730, CON:          0, MSG: PUB|A2|11067
PUB:      75904, CON:          0, MSG: PUB|A0|11174
PUB:      87180, CON:          0, MSG: PUB|A1|11276
PUB:      87180, CON:       7992, MSG: CON|A9|7992
PUB:      87180, CON:      15992, MSG: CON|A8|8000
PUB:      87180, CON:      23992, MSG: CON|A7|8000
PUB:      87180, CON:      31992, MSG: CON|A5|8000
PUB:      87180, CON:      39992, MSG: CON|A4|8000
PUB:      87180, CON:      47992, MSG: CON|A3|8000
PUB:      87180, CON:      55992, MSG: CON|A2|8000
PUB:      87180, CON:      64512, MSG: CON|A1|8520
PUB:      87180, CON:      78059, MSG: CON|A0|13547
PUB:      87180, CON:      86059, MSG: CON|A6|8000
```
For the test example above you can see that the final total was 87180 messages published and 86059 consumed. 
This shows that 1121 messages were lost.