#!/bin/sh

mkdocs build

if [ $? -ne 0 ]; then
    echo "mkdocs build failed"
    exit 1
fi

mkdir -p ../website/docs
cp -r site/* ../website/docs
