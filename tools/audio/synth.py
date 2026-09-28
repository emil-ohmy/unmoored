"""Small sound-synthesis kit for art/audio/*.py (run by tools/build-audio.sh with Blender's bundled Python,
which has numpy). Mono, 48 kHz, float arrays in -1..1; write() saves 16-bit WAV.

Building blocks: noise (white/brown), oscillators, time-varying filters (one-pole, RBJ band-pass), envelopes,
events scattered in time, and loop crossfading."""
import wave

import numpy as np

SR = 48000


def seconds(duration):
    return np.arange(int(duration * SR)) / SR


def white(duration, rng):
    return rng.uniform(-1.0, 1.0, int(duration * SR))


def brown(duration, rng):
    """Deep rumbly noise: integrated white noise with the drift removed."""
    x = np.cumsum(rng.uniform(-1.0, 1.0, int(duration * SR)))
    x = x - lowpass(x, 5.0)  # remove the slow wander
    return x / (np.max(np.abs(x)) + 1e-9)


def sine(freq, duration, phase=0.0):
    """freq may be a number or a per-sample array (a sweep)."""
    f = np.broadcast_to(np.asarray(freq, dtype=float), (int(duration * SR),))
    return np.sin(2 * np.pi * np.cumsum(f) / SR + phase)


def lowpass(x, cutoff):
    """One-pole low-pass; cutoff may be a number or a per-sample array."""
    c = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = 1.0 - np.exp(-2 * np.pi * c / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a[i] * (x[i] - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def bandpass(x, centre, q):
    """RBJ constant-peak band-pass; centre and q may be per-sample arrays (sweeps)."""
    f = np.broadcast_to(np.asarray(centre, dtype=float), x.shape)
    qs = np.broadcast_to(np.asarray(q, dtype=float), x.shape)
    w = 2 * np.pi * np.clip(f, 20, SR * 0.45) / SR
    alpha = np.sin(w) / (2 * qs)
    b0, a0, a1, a2 = alpha, 1 + alpha, -2 * np.cos(w), 1 - alpha
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i in range(len(x)):
        yi = (b0[i] * x[i] - b0[i] * x2 - a1[i] * y1 - a2[i] * y2) / a0[i]
        x2, x1, y2, y1 = x1, x[i], y1, yi
        y[i] = yi
    return y


def envelope(duration, attack, decay, curve=4.0, peak_at=None):
    """Rise over `attack` seconds (or to `peak_at`), then an exponential-ish fall to silence by `duration`."""
    t = seconds(duration)
    peak = attack if peak_at is None else peak_at
    rise = np.clip(t / max(peak, 1e-4), 0, 1) ** 1.5
    fall = np.clip(1 - (t - peak) / max(duration - peak, 1e-4), 0, 1) ** curve
    return np.where(t < peak, rise, fall)


def decay(duration, tau):
    return np.exp(-seconds(duration) / tau)


def glide(start, end, duration, shape=1.0):
    """Per-sample value from start to end (shape > 1 eases in)."""
    return start + (end - start) * np.linspace(0, 1, int(duration * SR)) ** shape


def arc(low, high, duration, peak_at=0.4):
    """Per-sample value rising low -> high by `peak_at` (fraction), then falling back towards low."""
    u = np.linspace(0, 1, int(duration * SR))
    rise = np.clip(u / peak_at, 0, 1)
    fall = np.clip((u - peak_at) / (1 - peak_at), 0, 1)
    shape = np.where(u < peak_at, np.sin(rise * np.pi / 2), np.cos(fall * np.pi / 2) * 0.8 + 0.2)
    return low + (high - low) * shape


def place(target, sound, at):
    """Add `sound` into `target` starting at `at` seconds (clipped to the end)."""
    start = int(at * SR)
    end = min(len(target), start + len(sound))
    if start < end:
        target[start:end] += sound[:end - start]
    return target


def pad(x, duration):
    out = np.zeros(int(duration * SR))
    n = min(len(x), len(out))
    out[:n] = x[:n]
    return out


def loop_crossfade(x, fade):
    """Make x loop seamlessly: fade its last `fade` seconds into its start, then drop them."""
    n = int(fade * SR)
    head, tail = x[:n].copy(), x[-n:]
    ramp = np.linspace(0, 1, n)
    out = x[:-n].copy()
    out[:n] = head * ramp + tail * (1 - ramp)
    return out


def finish(x, peak=0.89, fade_in=0.002, fade_out=0.01):
    """Normalise to `peak` and add tiny fades so there are no clicks at the ends."""
    x = x - np.mean(x)
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    n_in, n_out = int(fade_in * SR), int(fade_out * SR)
    if n_in:
        x[:n_in] *= np.linspace(0, 1, n_in)
    if n_out:
        x[-n_out:] *= np.linspace(1, 0, n_out)
    return x


def write(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(data.tobytes())
