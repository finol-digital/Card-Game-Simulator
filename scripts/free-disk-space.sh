#! /bin/sh

echo "Before free-disk-space.sh..."
df -h

sudo swapoff -a
sudo rm -f /swapfile
sudo apt clean
docker rmi $(docker images -q)

echo "After free-disk-space.sh..."
df -h
