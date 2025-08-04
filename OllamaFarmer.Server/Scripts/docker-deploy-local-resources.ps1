# Set current directory to the script's directory
	# $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
	# Set-Location $scriptDir
	# Set-Location ..


# Run this in parallel
	# docker run -d -v $env:APPDATA\ollama-docker:/root/.ollama -p 11434:11434 --name ollama ollama/ollama:latest
	# docker run --name ollama-farmer-mysql -v $env:APPDATA\OllamaFarmer\mysql:/var/lib/mysql -e MYSQL_ROOT_PASSWORD=ollama-farmer-mysql -p 3306:3306 -d mysql:latest
	#
	# PH=$(echo '<password>' | docker run --rm -i datalust/seq config hash)
	# docker run \
	#   --name ollama-farmer-seq \
	#   -d \
	#   --restart unless-stopped \
	#   -e ACCEPT_EULA=Y \
	#   -e SEQ_API_CANONICALURI=https://seq.example.com \
	#   -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
	#   -v $env:APPDATA/Seq/data:/data \
	#   # -p 80:80 \
	#   -p 5341:5341 \
	#   datalust/seq


# Fix Docker when WSL corrupts or something
# wsl unregister docker-desktop



# Check if Docker is running
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
	Write-Host "Docker is not installed or not in the PATH. Please install Docker and try again."
	exit 1
}


Write-Host "Starting Ollama"
$ollamaContainer = Start-Process -FilePath "docker" -ArgumentList "run", "-d", "-v", "$env:APPDATA\ollama-docker:/root/.ollama", "-p", "11434:11434", "--name", "ollama", "ollama/ollama:latest" -PassThru
Write-Host "Starting MySQL"
$mysqlContainer = Start-Process -FilePath "docker" -ArgumentList "run", "--name", "ollama-farmer-mysql", "-v", "$env:APPDATA\OllamaFarmer\mysql:/var/lib/mysql", "-e", "MYSQL_ROOT_PASSWORD=ollama-farmer-mysql", "-p", "3306:3306", "-d", "mysql:latest" -PassThru
Write-Host "Starting Seq"
$seqContainer = Start-Process -FilePath "docker" -ArgumentList "run", "--name", "ollama-farmer-seq", "-d", "--restart", "unless-stopped", "-e", "ACCEPT_EULA=Y", "-e", "SEQ_PASSWORD=Seq", "-v", "$env:APPDATA/Seq/data:/data", "-p", "5341:5341", "-p", "5342:80", "datalust/seq" -PassThru

Write-Host "Waiting for Ollama to start..."
$ollamaContainer.WaitForExit()
Write-Host "Waiting for MySQL to start..."
$mysqlContainer.WaitForExit()
Write-Host "Waiting for Seq to start..."
$seqContainer.WaitForExit()
Write-Host "Ollama, MySQL, and Seq containers started successfully."


#Set-Location $scriptDir

# Sleep 3 seconds
# Start-Sleep -Seconds 3
