@echo off

cd /D "%~dp0"

echo Generating flatbuffers header file...

.\flatbuffers-schema\flatc.exe --csharp --gen-all --gen-object-api --gen-onefile -o .\RLBot\Flat .\flatbuffers-schema\rlbot.fbs

IF EXIST .\RLBot\Flat\Flat.cs del .\RLBot\Flat\Flat.cs

REM the file produced is called rlbot_generated.cs, rename it to Flat.cs after removing the old one
ren .\RLBot\Flat\rlbot_generated.cs Flat.cs

echo Done.
