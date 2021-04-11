# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# exit when any command fails
set -e

# keep track of the last executed command
trap 'last_command=$current_command; current_command=$BASH_COMMAND' DEBUG
# echo an error message before exiting
trap 'echo "Command \"${last_command}\" failed with exit code $?!"; echo ""; echo "Factory Orchestrator might not be installed or configured correctly."' EXIT

if [ ! "$EUID" = 0 ]
then
    echo "sudo elevation required. Please re-run with sudo."
    trap '' EXIT
    exit 1
fi

SCRIPTDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# copy .service files
sudo cp -f $SCRIPTDIR/*.service /etc/systemd/system/
sudo systemctl daemon-reload

# delete old service files if present
if [ -d "/usr/sbin/FactoryOrchestrator" ]
then
    rm -r -f /usr/sbin/FactoryOrchestrator
fi

# unzip binary files
sudo unzip -q -d /usr/sbin/FactoryOrchestrator -o $SCRIPTDIR/Microsoft.FactoryOrchestrator.Service-10.0.0-foo-linux-x64-bin.zip
sudo cp -f $SCRIPTDIR/Microsoft.FactoryOrchestrator.CleanVolatile.sh /usr/sbin/FactoryOrchestrator/
# mark everything as executable
sudo chmod -R +x /usr/sbin/FactoryOrchestrator/*

echo ""
echo "The FactoryOrchestrator service is installed as a systemd service!"
echo "Binaies are located at /usr/sbin/FactoryOrchestrator/"
echo "Start it manually with: sudo systemctl start Microsoft.FactoryOrchestrator.service"
echo ""

if [ ! -z "$1" ]&& [ $1 = "enable" ];
then
    sudo systemctl enable Microsoft.FactoryOrchestrator.CleanVolatile.service
    sudo systemctl enable Microsoft.FactoryOrchestrator.service
    echo "The FactoryOrchestrator service is enabled! It will start on next boot or if started manually."
else
    echo "The service must be manually started. If you wish to later enable it to start on next boot, run:"
    echo "sudo systemctl enable Microsoft.FactoryOrchestrator.CleanVolatile.service; sudo systemctl enable Microsoft.FactoryOrchestrator.service"
fi
trap '' EXIT
