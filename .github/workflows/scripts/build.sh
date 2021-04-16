solutionName="Chip8Emulator"
projectName="Chip8Emulator.BlazorWasm"
projectFile="$projectName/$projectName.csproj"
destFolderName="chip8-emulator"

rm -rf ./build

echo "building $projectName ..."
buildPath="./build/$projectName-tmp"    
dotnet publish $projectFile --output $buildPath --configuration Release

mv -v $buildPath/wwwroot/* $buildPath/wwwroot/.* ./build      
rm -rf $buildPath

sed -i -e "s/<base href=\"\/\" \/>/<base href=\"\/$destFolderName\/\" \/>/g" ./build/index.html