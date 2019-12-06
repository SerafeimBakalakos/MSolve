@echo off
set /p np="Enter number of processes: "
dotnet publish -c Debug -r win10-x64
cd bin\Debug\netcoreapp2.2\win10-x64
mpiexec -n %np% ISAAR.MSolve.Solvers.Tests.exe
cd \..\..\..\...
PAUSE