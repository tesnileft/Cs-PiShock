# PiShock for C#

The _bester_ (And I think only (don't quote me on that)) C# API wrapper for the Pishock.
This is a C# adaptation of Zerario's [Python PiShock API](https://github.com/zerario/Python-PiShock)
> If you don't know what [PiShock](https://pishock.com/#/) is, you have no reason to be here.


## Features
- Not a CLI because thats not what this is for.
- Simple function names and all async operations offloaded to their own thread.
- Serial API ( With currently support for auto-detecting the port on windows only)
- Documentation comments on every (public) function and hopefully an actual documentation in the future

- To be added: Http API. (Which will be interchangable with the serial API)



## Install
Grab the [NuGet Package](https://www.nuget.org/packages/CsPiShock/) or simply copy and paste
```
dotnet add package CsPiShock
```
into your favorite terminal.
