This repository provides a .NET solution to test large numbers of bindings on RabbitMQ.

To build simply clone this repository. The `RmqBindingTest.sln` solution file should open with Visual Studio and restore and build without any further configuration.

To build self contained executables use the `dotnet` command as follows:
```
dotnet publish -o Output --self-contained -r win-x64
```

There are two console projects, `RmqBindingTest.Consumer` and `RmqBindingTest.Publisher`. I ran multiple instances of each
on two separate VMs using the following batch scripts:

Both console applications require the following environment variables to be set:
```
BINDING_TEST_RMQ_HOST
BINDING_TEST_RMQ_VHOST
BINDING_TEST_RMQ_USER
BINDING_TEST_RMQ_PASSWORD
```

For the publisher, to launch 10 instances, each publishing messages with routing keys from "00.0.0" to "99.9.9":
```
pushd <location of RmqBindingTest.Publisher.exe>

start "" "RmqBindingTest.Publisher.exe" A0 0000 9999
start "" "RmqBindingTest.Publisher.exe" A1 0000 9999
start "" "RmqBindingTest.Publisher.exe" A2 0000 9999
start "" "RmqBindingTest.Publisher.exe" A3 0000 9999
start "" "RmqBindingTest.Publisher.exe" A4 0000 9999
start "" "RmqBindingTest.Publisher.exe" A5 0000 9999
start "" "RmqBindingTest.Publisher.exe" A6 0000 9999
start "" "RmqBindingTest.Publisher.exe" A7 0000 9999
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