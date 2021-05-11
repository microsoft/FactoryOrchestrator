# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

if [ ! "$EUID" = 0 ]
then
    echo "sudo elevation required. Please re-run with sudo."
    exit 1
fi

echo "We're sorry to see you go! Please consider filing an issue on GitHub at https://github.com/microsoft/FactoryOrchestrator/issues with any questions, bugs, or feedback you have."
echo ""

sudo systemctl stop Microsoft.FactoryOrchestrator.service
rm -r -f /usr/sbin/FactoryOrchestrator > /dev/null 2>&1
rm -f /etc/systemd/system/Microsoft.FactoryOrchestrator.* > /dev/null 2>&1
sudo systemctl daemon-reload

echo "Deleted all files in/usr/sbin/FactoryOrchestrator/."
echo "Deleted service configuration."
