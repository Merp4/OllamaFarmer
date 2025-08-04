# Set current directory to the script's directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir
Set-Location ..
# Build the Docker image with context set to the parent directory
# This assumes that the Dockerfile is in the current directory
# and the context is the parent directory
# The Dockerfile is named "Dockerfile" and is located in the current directory
# The image will be tagged as "ollama-farmer"
docker stop ollama-farmer
docker rm ollama-farmer
docker run -d --name ollama-farmer -p 8080:8080 -p 8081:8081 -e ConnectionStrings__MySql="server=host.docker.internal;uid=root;pwd=ollama-farmer-mysql;database=ollamafarmer" -e OllamaApi__Host="http://host.docker.internal:11434" merrrp/ollama-farmer
Set-Location $scriptDir
