# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

if [[ -f "/var/log/FactoryOrchestrator/FactoryOrchestratorVolatileServiceStatus.xml" ]]
then
    rm /var/log/FactoryOrchestrator/FactoryOrchestratorVolatileServiceStatus.xml
fi
