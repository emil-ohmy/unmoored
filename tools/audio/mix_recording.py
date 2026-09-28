"""Mix the audio of a recorded demo session from its log (see RecordDemoTests), offline and in step with the frames.

    <blender python> tools/audio/mix_recording.py <recording dir>   ->  <recording dir>/mix.wav

Mirrors Unity's settings: linear roll-off between min and max distance, mixed by spatial blend; panning from
where the sound is relative to the listener (which never rotates: +X is right). Music follows its logged volume.
"""
import json
import os
import sys
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import synth  # noqa: E402

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SR = synth.SR


def load(asset_path):
    with wave.open(os.path.join(REPO, "game", asset_path)) as w:
        channels = w.getnchannels()
        data = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(float) / 32767
    return data.reshape(-1, channels).T if channels == 2 else np.vstack([data, data])


def gain_and_pan(dx, dz, dy, blend, near, far):
    d = np.sqrt(dx * dx + dy * dy + dz * dz)
    positional = np.clip((far - d) / max(far - near, 1e-3), 0, 1)
    positional = np.where(d <= near, 1.0, positional)
    gain = (1 - blend) + blend * positional
    pan = np.clip(dx / np.maximum(d, 1.0), -1, 1) * blend * 0.8
    return gain, pan


def apply_pan(stereo, pan):
    angle = (pan + 1) * np.pi / 4
    return np.vstack([stereo[0] * np.cos(angle) * np.sqrt(2), stereo[1] * np.sin(angle) * np.sqrt(2)])


def main(folder):
    log = json.load(open(os.path.join(folder, "recording.json")))
    frames = log["frames"]
    n = int(len(frames) / log["fps"] * SR)
    t_frames = np.array([f["t"] for f in frames])
    t = np.arange(n) / SR
    lx, ly, lz = (np.interp(t, t_frames, [f[k] for f in frames]) for k in ("x", "y", "z"))
    mix = np.zeros((2, n))

    music = load(log["music"])
    reps = int(np.ceil(n / music.shape[1]))
    mix += np.tile(music, reps)[:, :n] * np.interp(t, t_frames, [f["music"] for f in frames])

    cache = {}
    for loop in log["loops"]:
        clip = cache.setdefault(loop["clip"], load(loop["clip"]))
        start = int(loop["offset"] * SR) % clip.shape[1]
        reps = int(np.ceil((n + start) / clip.shape[1]))
        body = np.tile(clip, reps)[:, start:start + n]
        gain, pan = gain_and_pan(loop["x"] - lx, loop["z"] - lz, loop["y"] - ly, loop["blend"], loop["min"], loop["max"])
        mix += apply_pan(body * gain * loop["volume"], pan)

    for shot in log["shots"]:
        clip = cache.setdefault(shot["clip"], load(shot["clip"]))
        length = int(clip.shape[1] / shot["pitch"])  # pitch shifts speed, as in Unity
        src = np.linspace(0, clip.shape[1] - 1, length)
        body = np.vstack([np.interp(src, np.arange(clip.shape[1]), clip[c]) for c in range(2)])
        i = min(n - 1, int(shot["t"] * SR))
        gain, pan = gain_and_pan(shot["x"] - lx[i], shot["z"] - lz[i], shot["y"] - ly[i], shot["blend"], shot["min"], shot["max"])
        body = apply_pan(body * gain * shot["volume"], pan)
        end = min(n, i + body.shape[1])
        mix[:, i:end] += body[:, :end - i]

    peak = np.max(np.abs(mix))
    if peak > 0.95:
        mix = np.tanh(mix / peak * 1.4) / np.tanh(1.4) * 0.95  # gentle limiting, only if needed
    synth.write(os.path.join(folder, "mix.wav"), mix)
    print(f"[mix] {n / SR:.1f}s, {len(log['shots'])} sounds, {len(log['loops'])} loops, peak {peak:.2f}")


main(os.path.abspath(sys.argv[1]))
