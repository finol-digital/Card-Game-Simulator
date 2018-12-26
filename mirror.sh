#!/bin/bash

set -e

if [ ! "$1" == "--forked" ]; then
    # copy the script to tmp so we don't hold lock
    tmpfile=$(mktemp /tmp/mirror.sh.XXXXXX)
    cp -rf $0 $tmpfile

    exec sh $tmpfile --forked
fi


branch=`git rev-parse --abbrev-ref HEAD`

if ! git remote show mirror > /dev/null ; then

    git remote add -f -t 2018 --no-tags mirror https://github.com/vis2k/Mirror.git
else
    git fetch mirror
fi

git checkout mirror/2018
git subtree split --rejoin -P Assets/Mirror -b mirror 
git checkout $branch

if [ -d Assets/Mirror ] ; then
    git subtree merge -P Assets/Mirror mirror
else
    git subtree add -P Assets/Mirror mirror
fi

