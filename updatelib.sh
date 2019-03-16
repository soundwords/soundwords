#!/bin/bash
errorhandler() {
    # Modified verion of http://stackoverflow.com/a/4384381/352573
    errorcode=$?
    echo "Error $errorcode"
    echo "The command executing at the time of the error was"
    echo "$BASH_COMMAND"
    echo "on line ${BASH_LINENO[0]}"
    exit $errorcode
}

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LIB=${DIR}/lib
PATCHVERSION=$(($( date +%s ) / 86400))
TAG=v5.4

# From now on, catch errors
trap errorhandler ERR

echo "Cleaning up"
rm -rf "${LIB}/ServiceStack*"
mkdir -p "${LIB}/ServiceStack"

if [ -d "${LIB}/src/ServiceStack" ]; then
    echo "Deleting ServiceStack repo"
    rm -rf "${LIB}/src/ServiceStack"
fi

if [ -d "${LIB}/src/ServiceStack.OrmLite" ]; then
    echo "Deleting ServiceStack.OrmLite repo"
    rm -rf "${LIB}/src/ServiceStack.OrmLite"
fi

if [ -d "${LIB}/src/ServiceStack.Redis" ]; then
    echo "Deleting ServiceStack.Redis repo"
    rm -rf "${LIB}/src/ServiceStack.Redis"
fi

echo "Cloning ServiceStack repo"
git clone --depth 1 --branch ${TAG} https://github.com/ServiceStack/ServiceStack "${LIB}/src/ServiceStack"

echo "Cloning ServiceStack.OrmLite repo"
git clone --depth 1 --branch ${TAG} https://github.com/ServiceStack/ServiceStack.OrmLite "${LIB}/src/ServiceStack.OrmLite"

echo "Cloning ServiceStack.Redis repo"
git clone --depth 1 --branch ${TAG} https://github.com/ServiceStack/ServiceStack.Redis "${LIB}/src/ServiceStack.Redis"


pushd "${LIB}"

docker-compose build
docker-compose run --rm servicestack

popd
echo "Finished succesfully"
