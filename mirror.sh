#!/bin/bash


branch=`git rev-parse --abbrev-ref HEAD`

if ! git remote show mirror > /dev/null ; then

    git remote add -f -t 2018 --no-tags mirror https://github.com/vis2k/Mirror.git
    git merge -s ours --no-commit --allow-unrelated-histories mirror/2018
    git read-tree --prefix=Assets/Mirror -u mirror/2018:Assets/Mirror
else
    git fetch mirror
    git pull -s subtree mirror 2018
fi

