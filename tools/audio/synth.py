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
    """Make x loop seamlessly: fade its last `fade` seconds into its start, then drop them. Mono or stereo."""
    n = int(fade * SR)
    head, tail = x[..., :n].copy(), x[..., -n:]
    ramp = np.sqrt(np.linspace(0, 1, n))  # equal-power: no dip in loudness at the seam
    out = x[..., :-n].copy()
    out[..., :n] = head * ramp + tail * np.sqrt(1 - ramp ** 2)
    return out


def finish(x, peak=0.89, fade_in=0.002, fade_out=0.01):
    """Normalise to `peak` and add tiny fades so there are no clicks at the ends. Mono (N,) or stereo (2, N)."""
    x = x - np.mean(x, axis=-1, keepdims=True)
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    n_in, n_out = int(fade_in * SR), int(fade_out * SR)
    if n_in:
        x[..., :n_in] *= np.linspace(0, 1, n_in)
    if n_out:
        x[..., -n_out:] *= np.linspace(1, 0, n_out)
    return x


def write(path, x):
    """16-bit WAV; x is mono (N,) or stereo (2, N)."""
    channels = 1 if x.ndim == 1 else x.shape[0]
    frames = x if x.ndim == 1 else x.T.reshape(-1)  # interleave L R L R ...
    data = (np.clip(frames, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as f:
        f.setnchannels(channels)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(data.tobytes())


# ---------------------------------------------------------------- music: stereo, spectral noise, reverb, voices

def smooth_random(duration, rng, rate=0.5, low=0.0, high=1.0):
    """A slowly wandering value (about `rate` changes per second), smoothly interpolated, per sample."""
    n = int(duration * SR)
    points = rng.uniform(low, high, int(duration * rate) + 4)
    xs = np.linspace(0, n, len(points))
    walk = np.interp(np.arange(n), xs, points)
    k = max(1, int(SR / max(rate, 1e-3) / 2))
    kernel = np.hanning(2 * k + 1)
    padded = np.pad(walk, k, mode="edge")
    return fft_convolve(padded, kernel / kernel.sum())[k * 2:k * 2 + n]


def spectral_noise(duration, rng, magnitude, n_fft=2048, hop=512, chunk=400):
    """Noise whose spectrum changes over time. magnitude(times (F,), freqs (K,)) -> (F, K) gains.
    Built frame by frame (random phases, overlap-add), so moving filters cost no per-sample Python loop."""
    n = int(duration * SR)
    frames = n // hop + 4
    freqs = np.fft.rfftfreq(n_fft, 1 / SR)
    window = np.hanning(n_fft) / 1.5  # hann at 75% overlap sums to ~1.5
    out = np.zeros(frames * hop + n_fft)
    for start in range(0, frames, chunk):
        idx = np.arange(start, min(frames, start + chunk))
        mag = magnitude(idx * hop / SR, freqs)
        spec = mag * np.exp(1j * rng.uniform(0, 2 * np.pi, mag.shape))
        blocks = np.fft.irfft(spec, n=n_fft, axis=1) * window
        for row, i in enumerate(idx):
            out[i * hop:i * hop + n_fft] += blocks[row]
    return out[:n]


def fft_convolve(x, ir):
    n = len(x) + len(ir) - 1
    size = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:n]


def reverb_ir(duration, decay_time, rng, predelay=0.03, brightness=0.35):
    """A dark hall impulse response: decaying noise that loses its highs as it fades (stereo pair)."""
    t = seconds(duration)
    irs = []
    for _ in range(2):
        noise = rng.normal(0, 1, len(t))
        freqs = np.fft.rfftfreq(len(t), 1 / SR)
        noise = np.fft.irfft(np.fft.rfft(noise) / (1 + (freqs / (6000 * brightness + 500)) ** 2), len(t))
        ir = noise * np.exp(-t * 6.9 / decay_time)
        darker = lowpass(ir, 1800) * np.clip(t / (decay_time * 0.5), 0, 1)  # the tail gets duller
        ir = ir * (1 - np.clip(t / (decay_time * 0.5), 0, 1)) + darker
        ir = np.concatenate([np.zeros(int(predelay * SR)), ir])
        irs.append(ir / np.sqrt(np.sum(ir ** 2)))
    return irs


def formants(freqs, vowel):
    """Gain of a sung vowel at the given frequencies (three resonances)."""
    table = {"oo": [(320, 80, 1.0), (800, 100, 0.35), (2400, 160, 0.08)],
             "ah": [(730, 90, 1.0), (1100, 110, 0.6), (2450, 160, 0.15)],
             "oh": [(500, 80, 1.0), (850, 100, 0.45), (2400, 160, 0.1)]}
    g = np.zeros_like(np.asarray(freqs, dtype=float))
    for centre, width, amp in table[vowel]:
        g += amp * np.exp(-0.5 * ((freqs - centre) / width) ** 2)
    return g + 0.02


def voice(f0, duration, rng, vowel="oo", vibrato=0.005, max_freq=5000):
    """A wordless sung voice by additive synthesis: harmonics of f0 (number or per-sample array) shaped by
    vowel formants, with a slow, slightly irregular vibrato."""
    t = seconds(duration)
    base = np.broadcast_to(np.asarray(f0, dtype=float), t.shape)
    wobble = 1 + vibrato * np.sin(2 * np.pi * rng.uniform(4.6, 5.6) * t + rng.uniform(0, 6)) \
        + 0.5 * vibrato * (smooth_random(duration, rng, 0.8) * 2 - 1)
    phase = 2 * np.pi * np.cumsum(base * wobble) / SR
    out = np.zeros_like(t)
    typical = float(np.mean(base))
    for k in range(1, int(max_freq / typical) + 1):
        gain = formants(k * typical, vowel) / k ** 0.7
        out += gain * np.sin(k * phase + rng.uniform(0, 2 * np.pi))
    return out


def glass(freq, duration, rng):
    """A glass-harmonica / distant bell tone: a few slightly inharmonic partials with long decays."""
    t = seconds(duration)
    out = np.zeros_like(t)
    for ratio, amp, tau in ((1.0, 1.0, 2.6), (2.0, 0.3, 1.6), (3.01, 0.14, 1.0), (4.2, 0.07, 0.6)):
        out += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    return out * np.clip(t / 0.04, 0, 1)


def note(name):
    """'D4' -> Hz (A4 = 440)."""
    steps = {"C": -9, "C#": -8, "Db": -8, "D": -7, "D#": -6, "Eb": -6, "E": -5, "F": -4, "F#": -3, "Gb": -3, "G": -2,
             "G#": -1, "Ab": -1, "A": 0, "A#": 1, "Bb": 1, "B": 2}
    return 440.0 * 2 ** ((steps[name[:-1]] + 12 * (int(name[-1]) - 4)) / 12)


def stereo(left, right):
    return np.vstack([left, right])


def pan(x, position):
    """Constant-power pan of a mono signal: position -1 (left) .. +1 (right); may be per-sample."""
    angle = (np.asarray(position, dtype=float) + 1) * np.pi / 4
    return np.vstack([x * np.cos(angle), x * np.sin(angle)])
