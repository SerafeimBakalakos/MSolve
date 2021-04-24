@echo off
set /p np="Enter number of MPI processes: "
echo ---------------------------
echo Compiling code...
echo ---------------------------
dotnet publish -c Debug -r win10-x64
cd bin\Debug\netcoreapp3.1\win10-x64
echo ---------------------------
echo Running executable...
echo ---------------------------
mpiexec -n %np% MGroup.Solvers.Tests.exe %np%
cd ..\..\..\..
PAUSE