dotnet publish -c Debug -r win10-x64
cd bin\Debug\netcoreapp2.2\win10-x64
mpiexec -n 4 ISAAR.MSolve.LinearAlgebra.Distributed.Tests.exe
PAUSE