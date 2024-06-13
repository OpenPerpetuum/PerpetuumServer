# The Open Perpetuum Server on Linux

1.  Install **mono-complete** as described [here](https://www.mono-project.com/download/stable/#download-lin).
2.  Clone the main [repository](https://github.com/OpenPerpetuum/PerpetuumServer) with the server source code:
```sh
git clone https://github.com/OpenPerpetuum/PerpetuumServer
```
3.  Change the current directory:
```sh
cd PerpetuumServer
```
4. Apply the patch to the source code:
```sh
git apply ./src/dev4linux.patch
```
5. Download package manager - [nuget.exe](https://www.nuget.org/downloads)
```sh
wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -O ./.nuget/nuget.exe
```
5. Restore the packages required for the server:
```sh
mono ./.nuget/nuget.exe restore
```
6. Build the server for the required configuration (Release or Debug):
```sh
msbuild -m -v:quiet Perpetuum.sln -p:Configuration=Release
```
7. After setting up the database and data directory, the server is started with the command:
```sh
mono ./bin/x64/Release/Perpetuum.Server/Perpetuum.Server.exe <PATH_TO_DATA_DIR>
```
