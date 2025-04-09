#!/usr/bin/bash

cd "$(dirname "$0")"

echo Generating flatbuffers header file...

./flatbuffers-schema/flatc --gen-all --csharp --gen-object-api --gen-onefile -o ./RLBot/Flat ./flatbuffers-schema/rlbot.fbs

# the file produced is called rlbot_generated.cs, rename it to rlbot.cs after removing the old one
rm -f ./RLBot/Flat/Flat.cs
mv ./RLBot/Flat/rlbot_generated.cs ./RLBot/Flat/Flat.cs
sed -i 's/rlbot\.flat/RLBot.Flat/g' ./RLBot/Flat/Flat.cs

echo Done.
