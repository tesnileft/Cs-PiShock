# PiShock for C#

The _bester_ (And I think only (don't quote me on that)) C# Serial/Http API wrapper for the Pishock.
This is a C# adaptation of Zerario's [Python PiShock API](https://github.com/zerario/Python-PiShock)
> If you don't know what [PiShock](https://pishock.com/#/) is, you have no reason to be here.


## Features
- Simple function names and all async operations offloaded to their own thread.
- Serial API ( With currently support for auto-detecting the port on windows and linux)
- Documentation comments on every (public) function and hopefully an actual documentation in the future

- To be added: Http API. (Which will be interchangable with the serial API)

## Usage

```
PiShockSerialApi api = new PiShockSerialApi();	//This will try and find a pishock plugged into your computer. Alternatively you can provide it with a port.
SerialShocker shockerA = api.GetShocker(1111);	//Here put the ID of your shocker (you can find it on the website, or by doing api.Info() to get a list of shockers connected to this pishock)
shockerA.Shock(1000, 10); //Put in the time in ms to execute a command, and an intensity between 1-100
//Other options are Vibrate( Supports duration and intensity) , Beep (supports duration) and End (parameterless).

//To be implemented
PiShockHttpApi httpApi = new PiShockHttpApi("Name", "api-key", "Cool script.jpeg");		
//your login name, api key (obtained from the "Account" section on the website) and an optional name for the logs.
HttpShocker shockerB = httpApi.GetShocker(6969);
shockerB.Vibrate(3000, 21); //Put in the duration in ms, gets rounded to the nearest interval that the HTTP API accepts.

```

## Install
Grab the [NuGet Package](https://www.nuget.org/packages/CsPiShock/) or simply copy and paste
```
dotnet add package CsPiShock
```
into your favorite terminal.

