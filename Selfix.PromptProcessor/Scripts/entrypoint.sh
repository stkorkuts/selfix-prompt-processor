#!/bin/bash
set -e

echo "Starting vLLM server..."

# Start vLLM with direct console output
/app/start-vllm.sh &
VLLM_PID=$!
echo "vLLM process started with PID: $VLLM_PID"

# Wait a moment for initial startup
sleep 5

# Check if vLLM is still running
if ! ps -p $VLLM_PID > /dev/null; then
    echo "ERROR: vLLM process failed to start."
    exit 1
fi

echo "Waiting for vLLM to initialize..."
MAX_RETRIES=30
RETRY_COUNT=0

if [ "$Vllm__IsHighVram" = "true" ]; then
    RETRY_INTERVAL=10
else
    RETRY_INTERVAL=20
fi

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if ! ps -p $VLLM_PID > /dev/null; then
        echo "ERROR: vLLM process died during initialization."
        exit 1
    fi

    echo "Attempt $((RETRY_COUNT+1))/$MAX_RETRIES: Checking if vLLM server is responding..."
    RESPONSE=$(curl -s -m 5 "http://$Vllm__Host:$Vllm__Port/v1/models" 2>/dev/null || echo "")

    if [ ! -z "$RESPONSE" ]; then
        echo "vLLM server is ready!"
        echo "Response: $RESPONSE"
        break
    fi

    echo "vLLM not ready yet. Retrying in ${RETRY_INTERVAL} seconds..."
    sleep $RETRY_INTERVAL
    RETRY_COUNT=$((RETRY_COUNT + 1))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo "vLLM server failed to start within the expected time."
    exit 1
fi

# Start .NET application with direct console output
echo "Starting Selfix.PromptProcessor.EntryPoint..."
dotnet Selfix.PromptProcessor.EntryPoint.dll &
DOTNET_PID=$!

# Handle termination
trap 'kill $VLLM_PID $DOTNET_PID 2>/dev/null || true' SIGTERM SIGINT

# Monitor both processes
while kill -0 $VLLM_PID &>/dev/null && kill -0 $DOTNET_PID &>/dev/null; do
    sleep 2
done

# If we get here, something died
if ! kill -0 $VLLM_PID &>/dev/null; then
    echo "ERROR: vLLM service exited unexpectedly"
else
    echo "ERROR: .NET application exited unexpectedly"
fi

# Cleanup
kill $VLLM_PID $DOTNET_PID &>/dev/null || true
exit 1