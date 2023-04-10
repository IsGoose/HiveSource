# Hive
## Introduction
An Extension for Arma 2 Operation Arrowhead 1.64, Providing an Easy-to-use Interface for Interacting with a MySql Database.  
Designed to be Open Source, and to be used as a Solid Base for Adding your Own Database Operations, System Calls, Web Requests, and the like.  
##Note
While Hive is Designed to be a Base for Building your Own Extension, it can be Modified Directly, rather than used as a Dependency for your Own Extension

## Standalone Build Instructions
1) Clone Repository via git CLI or GitHub Application
2) Ensure Git LFS is Initialised
3) Open `HiveSource.sln` Solution File in IDE Of Choice (Pref. Visual Studio or Rider)
4) Restore Nuget Packages
5) Target 32-Bit(x86) Build (Operation Arrowhead is 32-Bit, So Extension Binary Must also be 32-Bit)
6) Build Hive Project
7) Build Output is Located At `SolutionRoot\Build\[Debug|Release]\Hive\`

## Usage
Hive is Designed to be used with Arma 2 Operation Arrowhead 1.64 (EOL) and to be called directly from SQF via `callExtension`. However, it is **not** designed to be used *out of the box*, but rather, to Serve as a Base for your Own Extension. Hive Provides everything you need to get started with Developing Hive for your Own Usage, such as Logging, Configuration, MySql Interfacing, and Abstractions to Make Quering your MySql Database Easier  

## So What Do you Need to Know?
A Basic Knowledge of C# would not go amiss here, though for Simply Adding your Own Methods the Concept is relatively easy to grasp.  
There is an Example `Controller` Provided (Appropriately Named `ExampleController`) Located at `\Hive\Controllers\ExampleController.cs` Which Provides Basic Examples for Database Calls.
### Setting Up
1) Build Hive (See [Standalone Build Instructions](#standalone-build-instructions))
2) Copy All Files (Except `HiveConfig.json`) from the Output Build Directory to the Root Directory of your Server Install (Same Folder that Contains `arma2oaserver.exe` for Example)
3) Copy the `HiveConfig.json` File to your Server Config Directory (Same Folder that Contains `basic.cfg` for Example)
4) Edit the `HiveConfig.json` to Match your MySql Login Credentials. See [Hive Configuration. INSERT LINK](INSERT_LINK_HERE)
5) From the Server, Execute the Following SQF: `"Hive" callExtension str ["System","Setup",true];`
5) If All Goes Well, Hive should not Be Running. Check the Server Console Window (Or External Console depending on Configuration) for Succesful Logs
6) Assuming Hive Setup was a Success, Hive can now be Used to call Any Other Methods.


## Still to Document
* Calling Format
* Return Format
* Quirks with Hive & `callExtension`
* Differentiating Method Types (void, typed, Synchronous, Asyncronous, Fire & Forget)
* Task System
* Parameter Parsing
* Supported Arma <--> C# Data Types via `ArrayParser`
* DB Abstraction Functions
* Developing Hive as-itself, or as a Dependency

## Still to Develop
* Provide Basic `LogController` Since Configuration Already Supports it
* Provide Hive SQF Wrapper Functions (Possibly include Scripted Callbacks?)

## Still In Active Development. More to Come..
