#!/bin/bash
export TS_DIR="$(dirname "${BASH_SOURCE[0]}")"
export VERSION=$(git describe --tags --abbrev=0 | sed -n 's/v\(.\+\)/\1/p')
cd $TS_DIR

# Delete the existing build if it exists
rm HolySigmar.zip

# Create our temp folders
mkdir -p TEMP/

# Copy the files into them
cp manifest.json TEMP/manifest.json
cp icon.png TEMP/icon.png
cp ../README.md TEMP/README.md
cp ../src/Plugin/bin/Release/net35/HolySigmar.dll TEMP/HolySigmar.dll

# Modify the version number
sed -i "s/{VERSION}/$VERSION/g" TEMP/manifest.json

# Zip the folder
cd TEMP
zip -9r ../HolySigmar.zip *

# Delete the temp dir
cd ..
rm -r TEMP
