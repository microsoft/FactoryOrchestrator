# exit when any command fails
set -e

# keep track of the last executed command
trap 'last_command=$current_command; current_command=$BASH_COMMAND' DEBUG
# echo an error message before exiting
trap 'echo "\"${last_command}\" command filed with exit code $?."' EXIT

SCRIPTDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# copy .service files
sudo cp $SCRIPTDIR/*.service /etc/systemd/system/
sudo systemctl daemon-reload

# copy binary files
echo "The FactoryOrchestrator service is installed as a systemd service!"
echo "Start it manually with: sudo systemctl start Microsoft.FactoryOrchestrator.service"

if [ $1 = "enable" ]; then
    sudo systemctl enable Microsoft.FactoryOrchestrator.CleanVolatile.service
    sudo systemctl enable Microsoft.FactoryOrchestrator.service
    echo "The FactoryOrchestrator service is enabled! It will start on next boot or if started manually."
else
    echo "The service must be manually started. If you wish to later enable it to start on next boot, run:"
    echp "sudo systemctl enable Microsoft.FactoryOrchestrator.CleanVolatile.service; sudo systemctl enable Microsoft.FactoryOrchestrator.service"
fi