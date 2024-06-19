#!/bin/bash

cd p2p || exit

./buildGoARM.sh

cd ../chat || exit

./buildDotNet.sh
