# Set current directory to the script's directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir
Set-Location ..
# Build the Docker image with context set to the parent directory
# This assumes that the Dockerfile is in the current directory
# and the context is the parent directory
# The Dockerfile is named "Dockerfile" and is located in the current directory
# The image will be tagged as "ollama-farmer"
docker build -t merrrp/ollama-farmer -f .\Dockerfile ..
Set-Location $scriptDir
