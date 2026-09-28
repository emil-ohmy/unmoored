"""An oar blow bouncing off a Laden's heap: a sharp transient, a hollow wooden knock (the chest), a short
metallic ring (its iron bands and the clock), and a rattle of loose things shifting."""
VARIANTS = 3

# Hz, decay time constant (s), amplitude. Short decays: a heap of junk, not a bell.
PARTIALS = [(612, 0.26, 0.5), (1047, 0.2, 0.35), (1562, 0.15, 0.3), (2215, 0.1, 0.2), (2987, 0.07, 0.15)]


def render(v, rng):
    duration = 0.8
    k = rng.uniform(0.94, 1.06)
    out = np.zeros(int(duration * synth.SR))
    click = synth.highpass(synth.white(0.008, rng), 2000) * synth.decay(0.008, 0.002) * 0.7
    knock = (synth.bandpass(synth.white(0.4, rng), 180 * k, 9) * synth.decay(0.4, 0.05)
             + synth.bandpass(synth.white(0.4, rng), 420 * k, 7) * synth.decay(0.4, 0.035)) * 9.0  # dies out: no click
    ring = np.zeros_like(out)
    for freq, tau, amp in PARTIALS:
        f = freq * k * rng.uniform(0.97, 1.03)
        ring += synth.sine(f * synth.glide(1.0, 0.985, duration), duration, rng.uniform(0, 6)) * synth.decay(duration, tau) * amp
    synth.place(out, knock, 0.0)
    out += ring * 0.45
    synth.place(out, click, 0.0)
    for _ in range(rng.integers(3, 6)):  # the heap settling
        tick = synth.bandpass(synth.white(0.03, rng), rng.uniform(1800, 3200), 4) * synth.decay(0.03, 0.006)
        synth.place(out, tick * rng.uniform(1.5, 3.0), rng.uniform(0.05, 0.22))
    return out
