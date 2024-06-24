#!/bin/bash

cd p2p || exit

./buildGoARM.sh

cd ../chat || exit

./buildDotNet.sh

cd ../ || exit
cp ./chat/out/* ./build

rm -r -d ./chat/out

cd ./Build || exit
chmod +x chat
