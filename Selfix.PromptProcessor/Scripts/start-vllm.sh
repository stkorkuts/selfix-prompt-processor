#!/bin/bash
set -e

if [ -z "$MODEL_PATH" ]; then
    echo "ERROR: MODEL_PATH environment variable is not set"
    exit 1
fi

echo "Starting vLLM with model path: $MODEL_PATH"
echo "vLLM host: $Vllm__Host"
echo "vLLM port: $Vllm__Port"
echo "High VRAM mode: $Vllm__IsHighVram"

# Set configurations based on VRAM availability
if [ "$Vllm__IsHighVram" = "true" ]; then
    echo "Using high VRAM configuration (24GB+ GPU)"
    MAX_MODEL_LEN=32768
    SWAP_SPACE=4
    GPU_MEM_UTIL=0.95
    BLOCK_SIZE=32
else
    echo "Using standard VRAM configuration (16GB GPU)"
    MAX_MODEL_LEN=16384
    SWAP_SPACE=2
    GPU_MEM_UTIL=0.9
    BLOCK_SIZE=16
fi

# Run with configuration based on VRAM setting
python3 -m vllm.entrypoints.openai.api_server \
  --model $MODEL_PATH \
  --host $Vllm__Host \
  --port $Vllm__Port \
  --gpu-memory-utilization $GPU_MEM_UTIL \
  --tensor-parallel-size 1 \
  --quantization gptq_marlin \
  --max-model-len $MAX_MODEL_LEN \
  --swap-space $SWAP_SPACE \
  --block-size $BLOCK_SIZE