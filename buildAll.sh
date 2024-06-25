#!/bin/bash

rm -d -r ./Build
mkdir Build

cd p2p || exit

chmod +x buildGoARM.sh

./buildGoARM.sh

cd ../chat || exit

chmod +x buildDotNet.sh

./buildDotNet.sh

cd ../ || exit
cp ./chat/out/* ./build

rm -r -d ./chat/out

cd ./Build || exit
chmod +x chat
