#!/bin/bash

dirs=("calorie_agent")

cd ./service

for dir in $dirs
do
  mkdir -p $dir
  cd $dir
  python -m grpc_tools.protoc -I../../protos --python_out=. --pyi_out=. --grpc_python_out=. ../../protos/$dir.proto
  cd ..
done

