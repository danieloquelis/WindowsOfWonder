import asyncio
import websockets
import json
import base64
import numpy as np
from faster_whisper import WhisperModel

SAMPLE_RATE = 16000  # must match Unity MicrophoneStreamer
MODEL_SIZE  = "base.en" # use "tiny" for faster tests

model = WhisperModel(MODEL_SIZE, device="cpu")  # CPU is fine and stable on macOS

async def handle_client(websocket):
    print("Client connected")
    audio_chunks = []  # list of np.float32 chunks (mono, 16kHz)

    async for message in websocket:
        try:
            data = json.loads(message)

            # AUDIO CHUNK
            if "audio" in data:
                # Base64 -> bytes -> int16 -> float32 [-1,1]
                pcm_bytes = base64.b64decode(data["audio"])
                if len(pcm_bytes) == 0:
                    continue

                # int16 little-endian to float32
                # (PCM16 is signed little-endian from Unity code)
                samples_i16 = np.frombuffer(pcm_bytes, dtype=np.int16)
                samples_f32 = samples_i16.astype(np.float32) / 32768.0
                audio_chunks.append(samples_f32)
                # Optional debug:
                # print(f"Chunk received: {len(samples_f32)} samples")

            # STOP EVENT -> transcribe everything buffered
            elif data.get("eventType") == "stop":
                print("Stop event received")
                if not audio_chunks:
                    await websocket.send(json.dumps({"error": "No audio received"}))
                    continue

                full_audio = np.concatenate(audio_chunks, axis=0)
                audio_chunks = []  # reset for next utterance

                # faster-whisper expects 16kHz float32 mono
                segments, _ = model.transcribe(full_audio, beam_size=1)
                transcript = " ".join(seg.text for seg in segments).strip() or "[no speech detected]"

                print("Transcript:", transcript)
                await websocket.send(json.dumps({"text": transcript}))

            else:
                await websocket.send(json.dumps({"error": "Invalid message (no 'audio' or 'eventType')"}))

        except Exception as e:
            print("Error:", e)
            await websocket.send(json.dumps({"error": str(e)}))

async def main():
    # websockets>=11 handler signature: (websocket)
    async with websockets.serve(handle_client, "0.0.0.0", 8000, max_size=20_000_000):
        print("Server running at ws://0.0.0.0:8000")
        await asyncio.Future()

asyncio.run(main())
